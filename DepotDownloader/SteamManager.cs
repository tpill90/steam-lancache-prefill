using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using DepotDownloader.Handlers;
using DepotDownloader.Models;
using DepotDownloader.Protos;
using DepotDownloader.Settings;
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using Utf8Json;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader
{
    //TODO document
    public class SteamManager
    {
        private readonly IAnsiConsole _ansiConsole;

        private readonly Steam3Session _steam3;
        private readonly CdnPool _cdnPool;

        private readonly DownloadHandler _downloadHandler;
        private readonly ManifestHandler _manifestHandler;
        private readonly DepotHandler _depotHandler;
        private readonly AppInfoHandler _appInfoHandler;

        public SteamManager(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
            // Create required folders
            Directory.CreateDirectory(AppConfig.ConfigDir);
            Directory.CreateDirectory(AppConfig.ManifestCacheDir);

            _steam3 = new Steam3Session(_ansiConsole);
            _cdnPool = new CdnPool(_ansiConsole, _steam3);
            _appInfoHandler = new AppInfoHandler(_ansiConsole, _steam3);
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            _manifestHandler = new ManifestHandler(_cdnPool, _steam3);
            _depotHandler = new DepotHandler(_ansiConsole, _steam3, _appInfoHandler);

            AccountSettingsStore.LoadFromFile();
        }

        /// <summary>
        /// Logs the user into the Steam network, and retrieves available CDN servers and account licenses.
        ///
        /// Required to be called first before using SteamManager class.
        /// </summary>
        public void Initialize(string username)
        {
            using var timer = new AutoTimer(_ansiConsole, "Initialization complete...");
            
            _steam3.LoginToSteam(username);
            
            // Loading available licenses(games) for the current user
            _steam3.LoadAccountLicenses();
        }

        public async Task DownloadMultipleAppsAsync(List<uint> appIdsToDownload, DownloadArguments downloadArgs)
        {
            var distinctAppIds = appIdsToDownload.Distinct().OrderBy(e => e).ToList();

            // Need to load the latest app information from steam first
            await RetrieveAppMetadata(distinctAppIds);

            // Now we will be able to determine which apps can't be downloaded
            var availableApps = await _appInfoHandler.FilterUnavailableApps(distinctAppIds);
            
            foreach (var app in availableApps)
            {
                try
                {
                    await DownloadSingleAppAsync(app.AppId, downloadArgs);
                }
                catch (Exception e)
                {
                    // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                    _ansiConsole.MarkupLine(Red($"   Unexpected download error : {e.Message}"));
                }
            }
            _ansiConsole.LogMarkupLine($"Prefilled {availableApps.Count}  apps");
        }

        private async Task DownloadSingleAppAsync(uint appId, DownloadArguments downloadArgs)
        {
            AppInfoShim appInfo = await _appInfoHandler.GetAppInfo(appId);
            _ansiConsole.LogMarkup($"Starting {Cyan(appInfo.Common.Name)} - {White(appInfo.AppId)}");

            // Get all depots, and filter out any unavailable depots.
            var allDepots = appInfo.Depots;
            var validDepots = _depotHandler.RemoveInvalidDepots(allDepots);

            // Filter depots based on specified lang/os/architecture/etc
            var filteredDepots = _depotHandler.FilterDepotsToDownload(downloadArgs, validDepots);
            if (!filteredDepots.Any())
            {
                _ansiConsole.MarkupLine(Yellow("  No depots to download.  Current arguments filtered all depots"));
                return;
            }

            await _depotHandler.BuildLinkedDepotInfo(filteredDepots, appInfo);

            // We will want to re-download the entire app, if any of the depots have been updated
            if (downloadArgs.Force == false && !_depotHandler.AppHasUpdatedDepots(filteredDepots))
            {
                _ansiConsole.MarkupLine(Green("  Up to date!"));
                return;
            }
            _ansiConsole.Write("\n");

            await _cdnPool.PopulateAvailableServers();

            // Get the full file list for each depot, and queue up the required chunks
            var chunkDownloadQueue = await BuildChunkDownloadQueue(filteredDepots);

            // Finally run the queued downloads
            var downloadTimer = Stopwatch.StartNew();
            var totalBytes = ByteSize.FromBytes(chunkDownloadQueue.Sum(e => e.CompressedLength));
            _ansiConsole.LogMarkupLine($"Downloading {Magenta(totalBytes.ToDecimalString())} from {Yellow(chunkDownloadQueue.Count)} chunks");

            var downloadSuccessful = await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue);
            if (downloadSuccessful)
            {
                _depotHandler.MarkDownloadAsSuccessful(filteredDepots);
            }

            downloadTimer.Stop();
            var averageSpeed = ByteSize.FromBytes(totalBytes.Bytes / downloadTimer.Elapsed.TotalSeconds);
            var averageSpeedBits = $"{(averageSpeed.MegaBytes * 8).ToString("0.##")} Mbit/s";
            _ansiConsole.LogMarkupLine($"Downloaded in {Yellow(downloadTimer.Elapsed.ToString(@"h\:mm\:ss\.FFFF"))}.  Average speed : {Magenta(averageSpeedBits)}");
            
            _ansiConsole.WriteLine();
        }

        /// <summary>
        /// Gets the latest app metadata from steam, for the specified apps, as well as their related DLC apps
        /// </summary>
        private async Task RetrieveAppMetadata(List<uint> appIds)
        {
            //TODO is there a way to speed this up by cacheing any of the data locally?  For example, we could filter out tools ahead of time, to reduce the # of items requested
            using var timer = new AutoTimer(_ansiConsole, $"Retrieved info for {Magenta(appIds.Count)} apps");
            await _ansiConsole.StatusSpinner().StartAsync("Retrieving latest App info...", async _ =>
            {
                await _appInfoHandler.BulkLoadAppInfos(appIds);
                // Once we have our info, we can also load information for related DLC
                
                await _appInfoHandler.BulkLoadAppInfos(_appInfoHandler.GetOwnedDlcAppIds());
                await _appInfoHandler.BuildDlcDepotList();
            });
        }

        //TODO document
        private async Task<List<QueuedRequest>> BuildChunkDownloadQueue(List<DepotInfo> depots)
        {
            // Fetch all the manifests for each depot in parallel, as individually they can take a long time, 
            var depotManifests = new ConcurrentBag<ProtoManifest>();
            await _ansiConsole.StatusSpinner().StartAsync("Fetching depot manifests...", async _ =>
            {
                await Parallel.ForEachAsync(depots, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (depot, _) =>
                {
                    var manifest = await _manifestHandler.GetManifestFile(depot);
                    depotManifests.Add(manifest);
                });
            });
            
            var chunkQueue = new List<QueuedRequest>();
            // Queueing up chunks for each depot
            foreach (var depotManifest in depotManifests)
            {
                // A depot will contain multiple files, that are broken up into 1MB chunks
                var dedupedChunks = depotManifest.Files
                                             .SelectMany(e => e.Chunks)
                                             // Steam appears to do block level deduplication, so it is possible for multiple files to have the same chunk
                                             .DistinctBy(e => e.ChunkID)
                                             .ToList();
                foreach (var chunk in dedupedChunks)
                {
                    chunkQueue.Add(new QueuedRequest(depotManifest, chunk));
                }
            }
            return chunkQueue;
        }

        public HashSet<uint> GetAllUserAppIds()
        {
            //TODO there has to be a better way to know all the owned games, without including the invalid ones. Might be able to use the steam web api to do this.
            return _steam3.OwnedAppIds;
        }

        //TODO better name
        //TODO is there any way to possibly speed this up, without having to query steam?
        //TODO once apps are selected, they should be used by default, in addition to any additional params passed by the user
        private string _selectedAppsPath = $"{AppConfig.ConfigDir}/selectedAppsToPrefill.json";
        public async Task SelectApps()
        {
            var allApps = _steam3.OwnedAppIds.ToList();

            // Need to load the latest app information from steam, so that we have an updated list of all owned games
            await RetrieveAppMetadata(allApps);
            var availableApps = await _appInfoHandler.FilterUnavailableApps(allApps);

            AnsiConsole.Write(new Rule());

            var multiSelect = new MultiSelectionPrompt<AppInfoShim>()
                              .Title("Please select apps to prefill..")
                              .NotRequired()
                              .PageSize(25)
                              .MoreChoicesText(Grey("(Use ↑/↓ to navigate.  Page Up/Page Down skips pages)"))
                              .InstructionsText("[grey](Press [blue]<space>[/] to toggle an app, " + $"{Green("<enter>")} to accept)[/]")
                              .AddChoices(availableApps);

            // Restoring previously selected items
            if (File.Exists(_selectedAppsPath))
            {
                var previouslySelectedIds = JsonSerializer.Deserialize<List<uint>>(File.ReadAllText(_selectedAppsPath));
                foreach (var id in previouslySelectedIds)
                {
                    var appInfo = availableApps.First(e => e.AppId == id);
                    multiSelect.Select(appInfo);
                }
            }
                

            var selectedApps = AnsiConsole.Prompt(multiSelect);

            File.WriteAllText(_selectedAppsPath, JsonSerializer.ToJsonString(selectedApps.Select(e => e.AppId)));
            Debugger.Break();
        }
    }
}