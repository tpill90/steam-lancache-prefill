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
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader
{
    //TODO document
    public class SteamManager
    {
        private readonly IAnsiConsole _ansiConsole;

        //TODO remove static
        public static AppConfig Config = new AppConfig();

		//TODO make private again
        public Steam3Session _steam3;
        private Credentials _steam3Credentials;
        private CdnPool _cdnPool;

        private DownloadHandler _downloadHandler;
        private ManifestHandler _manifestHandler;
        private DepotHandler _depotHandler;
        private AppInfoHandler _appInfoHandler;

        public SteamManager(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
            // Create required folders
            Directory.CreateDirectory(AppConfig.ConfigDir);
            Directory.CreateDirectory(AppConfig.ManifestCacheDir);
        }

        // TODO comment
        // TODO need to test an account with steam guard
        public async Task Initialize(string username, string password, bool rememberPassword)
        {
            var timer = Stopwatch.StartNew();

            // capture the supplied password in case we need to re-use it after checking the login key
            Config.SuppliedPassword = password;
            ConnectToSteam(username, password, rememberPassword);
            
            // Populating available CDN servers
            _cdnPool = new CdnPool(_ansiConsole, _steam3);
            await _cdnPool.PopulateAvailableServers();

            // Loading available licenses(games) for the current user
            _steam3.LoadAccountLicenses();

            // Initializing our various classes now that Steam is connected.
            _appInfoHandler = new AppInfoHandler(_ansiConsole, _steam3);
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            _manifestHandler = new ManifestHandler(_cdnPool, _steam3);
            _depotHandler = new DepotHandler(_ansiConsole, _steam3, _appInfoHandler);
            
            _ansiConsole.LogMarkupLine("Initialization complete...", timer.Elapsed);
        }

        //TODO wrap in a spectre status?
        //TODO document
        private void ConnectToSteam(string username, string password, bool shouldRememberPassword)
        {
            Config.RememberPassword = shouldRememberPassword;
            string loginKey = null;

            if (username != null && Config.RememberPassword)
            {
                _ = AccountSettingsStore.Instance.LoginKeys.TryGetValue(username, out loginKey);
            }

            var logOnDetails = new SteamUser.LogOnDetails
            {
                Username = username,
                Password = loginKey == null ? password : null,
                ShouldRememberPassword = Config.RememberPassword,
                LoginKey = loginKey,
                LoginID = 0x534B32
            };

            _steam3 = new Steam3Session(logOnDetails, _ansiConsole);

            _ansiConsole.CreateSpectreStatusSpinner().Start("Connecting to Steam", _ =>
            {
                _steam3.ConnectToSteam();
            });

            _ansiConsole.CreateSpectreStatusSpinner().Start("Logging into steam", _ =>
            {
                _steam3.LoginToSteam();
                _steam3Credentials = _steam3.WaitForCredentials();
            });

            if (!_steam3Credentials.IsValid)
            {
                //TODO better exception type
                _ansiConsole.MarkupLine($"{Red("Error: Login to Steam failed")}");
                throw new Exception("Unable to get steam3 credentials.");
            }
        }

        public async Task DownloadMultipleAppsAsync(List<uint> appIdsToDownload)
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
                    // TODO need to implement the rest of the cli parameters
                    await DownloadSingleAppAsync(new DownloadArguments { AppId = app.AppId });
                }
                catch (Exception e)
                {
                    // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                    _ansiConsole.MarkupLine(Red("   Unexpected download error : " + e.Message));
                }
            }
            
            _ansiConsole.LogMarkupLine($"Prefilled {availableApps.Count}  apps");
        }

        /// <summary>
        /// Gets the latest app metadata from steam, for the specified apps, as well as their related DLC apps
        /// </summary>
        private async Task RetrieveAppMetadata(List<uint> appIds)
        {
            var timer = Stopwatch.StartNew();
            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Retrieving latest App info...", async _ =>
            {
                await _appInfoHandler.BulkLoadAppInfos(appIds);
                // Once we have our info, we can also load information for related DLC

                //TODO loading this DLC info can be pretty slow
                await _appInfoHandler.BulkLoadAppInfos(_appInfoHandler.GetOwnedDlcAppIds());
                await _appInfoHandler.BuildDlcDepotList();
            });
            _ansiConsole.LogMarkupLine($"Retrieved info for {Magenta(appIds.Count)} apps", timer.Elapsed);
        }

        private async Task DownloadSingleAppAsync(DownloadArguments downloadArgs)
        {
            _steam3.ThrowIfNotConnected();

            AppInfoShim appInfo = await _appInfoHandler.GetAppInfo(downloadArgs.AppId);
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
            if (!_depotHandler.AppHasUpdatedDepots(filteredDepots))
            {
                _ansiConsole.MarkupLine(Green("  Up to date!"));
                return;
            }
            _ansiConsole.Write("\n");

            // Get the full file list for each depot, and queue up the required chunks
            var chunkDownloadQueue = await BuildChunkDownloadQueue(filteredDepots);

            // Finally run the queued downloads
            var totalBytes = ByteSize.FromBytes(chunkDownloadQueue.Sum(e => e.CompressedLength));
            //TODO total download size is the wrong unit.
            _ansiConsole.LogMarkupLine($"Downloading {Magenta(totalBytes.ToDecimalString())} from {Yellow(chunkDownloadQueue.Count)} chunks");
            await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue);

            //TODO determine if there were any errors
            _depotHandler.MarkDownloadAsSuccessful(filteredDepots);

            _ansiConsole.WriteLine();
        }

        //TODO document
        //TODO cleanup
        //TODO implement logic that skips depots if they have previously been downloaded
        private async Task<List<QueuedRequest>> BuildChunkDownloadQueue(List<DepotInfo> depots)
        {
            // Fetch all the manifests for each depot in parallel, as individually they can take a long time, 
            var depotManifests = new ConcurrentBag<ProtoManifest>();
            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Fetching depot manifests...", async _ =>
            {
                await Parallel.ForEachAsync(depots, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (depot, _) =>
                {
                    var manifest = await _manifestHandler.GetManifestFile(depot);
                    depotManifests.Add(manifest);
                });
            });

            //TODO benchmark performance
            var stopwatch = Stopwatch.StartNew();
            var queue = new List<QueuedRequest>();
            // Queueing up chunks for each depot
            foreach (var depotManifest in depotManifests)
            {
                // A depot will contain multiple files, that are broken up into 1MB chunks
                var chunks = depotManifest.Files.SelectMany(e => e.Chunks).ToList();
                var disctint = chunks.DistinctBy(e => e.ChunkID).ToList();
                var newChunks = disctint.Select(e => new QueuedRequest
                {
                    DepotId = depotManifest.DepotId,
                    ChunkID = e.ChunkID,
                    CompressedLength = e.CompressedLength
                }).ToList();

                queue.AddRange(newChunks);
            }
            stopwatch.Stop();
            _ansiConsole.LogMarkupLine("Built queue", stopwatch.Elapsed);
            return queue;
        }
    }
}
