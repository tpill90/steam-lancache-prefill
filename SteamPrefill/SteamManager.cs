using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ByteSizeLib;
using SteamPrefill.Handlers;
using SteamPrefill.Models;
using SteamPrefill.Settings;
using SteamPrefill.Utils;
using Spectre.Console;
using SteamPrefill.Handlers.Steam;
using SteamPrefill.Models.Protos;
using Utf8Json;
using static SteamPrefill.Utils.SpectreColors;

namespace SteamPrefill
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
            

            _steam3 = new Steam3Session(_ansiConsole);
            _cdnPool = new CdnPool(_ansiConsole, _steam3);
            _appInfoHandler = new AppInfoHandler(_ansiConsole, _steam3);
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            _manifestHandler = new ManifestHandler(_ansiConsole, _cdnPool, _steam3);
            _depotHandler = new DepotHandler(_steam3, _appInfoHandler);

            UserAccountStore.LoadFromFile();
        }

        private bool _isInitialized;
        /// <summary>
        /// Logs the user into the Steam network, and retrieves available CDN servers and account licenses.
        ///
        /// Required to be called first before using SteamManager class.
        /// </summary>
        public void Initialize()
        {
            _steam3.LoginToSteam();
            
            // Loading available licenses(games) for the current user
            _steam3.LoadAccountLicenses();

            _isInitialized = true;
            _ansiConsole.LogMarkupLine("Steam session initialization complete!");
        }

        public async Task DownloadMultipleAppsAsync(List<uint> appIdsToDownload, DownloadArguments downloadArgs)
        {
            var timer = Stopwatch.StartNew();

            var distinctAppIds = appIdsToDownload.Distinct().OrderBy(e => e).ToList();

            // Need to load the latest app information from steam first
            await RetrieveAppMetadataAsync(distinctAppIds);

            // Now we will be able to determine which apps can't be downloaded
            var availableApps = await _appInfoHandler.FilterUnavailableAppsAsync(distinctAppIds);
            
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
                    _ansiConsole.MarkupLine("");
                }
            }

            _ansiConsole.MarkupLine("");
            _ansiConsole.LogMarkupLine($"Prefill complete! Prefilled {Magenta(availableApps.Count)} apps in {LightYellow(timer.FormattedElapsedString())}");
        }

        private async Task DownloadSingleAppAsync(uint appId, DownloadArguments downloadArgs)
        {
            AppInfoShim appInfo = await _appInfoHandler.GetAppInfoAsync(appId);
            _ansiConsole.LogMarkup($"Starting {Cyan(appInfo)}");

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

            await _depotHandler.BuildLinkedDepotInfoAsync(filteredDepots, appInfo);

            // We will want to re-download the entire app, if any of the depots have been updated
            if (downloadArgs.Force == false && !_depotHandler.AppHasUpdatedDepots(filteredDepots))
            {
                _ansiConsole.MarkupLine(Green("  Up to date!"));
                return;
            }
            _ansiConsole.Write("\n");

            await _cdnPool.PopulateAvailableServersAsync();

            // Get the full file list for each depot, and queue up the required chunks
            var chunkDownloadQueue = await BuildChunkDownloadQueueAsync(filteredDepots);

            // Finally run the queued downloads
            var downloadTimer = Stopwatch.StartNew();
            var totalBytes = ByteSize.FromBytes(chunkDownloadQueue.Sum(e => e.CompressedLength));
            _ansiConsole.LogMarkup($"Downloading {Magenta(totalBytes.ToDecimalString())}");
#if DEBUG
            _ansiConsole.Markup($" from {Yellow(chunkDownloadQueue.Count)} chunks");
#endif
            _ansiConsole.MarkupLine("");

            var downloadSuccessful = await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue);
            if (downloadSuccessful)
            {
                _depotHandler.MarkDownloadAsSuccessful(filteredDepots);
            }
            downloadTimer.Stop();

            // Logging some metrics about the download
            var averageSpeed = ByteSize.FromBytes(totalBytes.Bytes / downloadTimer.Elapsed.TotalSeconds);
            var averageSpeedBits = $"{(averageSpeed.MegaBytes * 8).ToString("0.##")} Mbit/s";
            _ansiConsole.LogMarkupLine($"Finished in {Yellow(downloadTimer.FormattedElapsedString())} - {Magenta(averageSpeedBits)}");
            _ansiConsole.WriteLine();
        }

        /// <summary>
        /// Gets the latest app metadata from steam, for the specified apps, as well as their related DLC apps
        /// </summary>
        private async Task RetrieveAppMetadataAsync(List<uint> appIds)
        {
            await _ansiConsole.StatusSpinner().StartAsync("Retrieving latest App info...", async _ =>
            {
                await _appInfoHandler.BulkLoadAppInfosAsync(appIds);

                // Once we have our info, we can also load information for related DLC
                await _appInfoHandler.BulkLoadAppInfosAsync(_appInfoHandler.GetOwnedDlcAppIds());
                await _appInfoHandler.BuildDlcDepotListAsync();
            });
            _ansiConsole.LogMarkupLine("Retrieved latest app metadata");
        }

        //TODO document
        private async Task<List<QueuedRequest>> BuildChunkDownloadQueueAsync(List<DepotInfo> depots)
        {
            var depotManifests = await _manifestHandler.GetAllManifestsAsync(depots);
            
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
            if (!_isInitialized)
            {
                throw new Exception("Steam session not initialized!");
            }
            //TODO there has to be a better way to know all the owned games, without including the invalid ones. Might be able to use the steam web api to do this.
            return _steam3.OwnedAppIds;
        }

        //TODO better name
        //TODO is there any way to possibly speed this up, without having to query steam?
        //TODO document
        private readonly string _selectedAppsPath = $"{AppConfig.ConfigDir}/selectedAppsToPrefill.json";
        public async Task SelectAppsAsync()
        {
            var allApps = _steam3.OwnedAppIds.ToList();

            // Need to load the latest app information from steam, so that we have an updated list of all owned games
            await RetrieveAppMetadataAsync(allApps);
            var availableApps = await _appInfoHandler.FilterUnavailableAppsAsync(allApps);

            _ansiConsole.Write(new Rule());

            var multiSelect = new MultiSelectionPrompt<AppInfoShim>()
                              .Title(Underline(White("Select apps to prefill...")))
                              .NotRequired()
                              .PageSize(35)
                              .MoreChoicesText(Grey("(Use ↑/↓ to navigate.  Page Up/Page Down skips pages)"))
                              .InstructionsText(Grey($"(Press {Blue("<space>")} to toggle an app, {Green("<enter>")} to accept)"))
                              .AddChoices(availableApps);

            // Restoring previously selected items
            foreach (var id in LoadPreviouslySelectedApps())
            {
                var appInfo = availableApps.First(e => e.AppId == id);
                multiSelect.Select(appInfo);
            }
                
            var selectedApps = _ansiConsole.Prompt(multiSelect);
            await File.WriteAllTextAsync(_selectedAppsPath, JsonSerializer.ToJsonString(selectedApps.Select(e => e.AppId)));

            _ansiConsole.MarkupLine($"Selected {Magenta(selectedApps.Count)} apps to prefill!  ");
            
            var exeCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @".\SteamPrefill.exe" : @"./SteamPrefill";
            _ansiConsole.MarkupLine("Apps will automatically be included by running " + Yellow($"{exeCommand} prefill"));
        }

        private List<uint> _previouslySelectedApps;
        public List<uint> LoadPreviouslySelectedApps()
        {
            if (_previouslySelectedApps == null)
            {
                if (File.Exists(_selectedAppsPath))
                {
                    _previouslySelectedApps = JsonSerializer.Deserialize<List<uint>>(File.ReadAllText(_selectedAppsPath));
                }
                else
                {
                    _previouslySelectedApps = new List<uint>();
                }
            }
            return _previouslySelectedApps;
        }
    }
}