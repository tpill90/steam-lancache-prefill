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
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader
{
    //TODO document
    public class SteamManager
    {
        private readonly IAnsiConsole _ansiConsole;

		//TODO make private again
        public Steam3Session _steam3;
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
            _steam3 = new Steam3Session(_ansiConsole);
        }

        /// <summary>
        /// Logs the user into the Steam network, and retrieves available CDN servers and account licenses.
        ///
        /// Required to be called first before using SteamManager class.
        /// </summary>
        public async Task Initialize(string username)
        {
            var timer = Stopwatch.StartNew();
            
            _steam3.LoginToSteam(username);
            
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
                    // TODO need to implement the rest of the cli parameters
                    await DownloadSingleAppAsync(app.AppId, downloadArgs);
                }
                catch (Exception e)
                {
                    // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                    _ansiConsole.MarkupLine(Red("   Unexpected download error : " + e.Message));
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
            var timer = Stopwatch.StartNew();
            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Retrieving latest App info...", async _ =>
            {
                await _appInfoHandler.BulkLoadAppInfos(appIds);
                // Once we have our info, we can also load information for related DLC
                
                await _appInfoHandler.BulkLoadAppInfos(_appInfoHandler.GetOwnedDlcAppIds());
                await _appInfoHandler.BuildDlcDepotList();
            });
            _ansiConsole.LogMarkupLine($"Retrieved info for {Magenta(appIds.Count)} apps", timer.Elapsed);
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
                    chunkQueue.Add(new QueuedRequest
                    {
                        DepotId = depotManifest.DepotId,
                        ChunkID = chunk.ChunkID,
                        CompressedLength = chunk.CompressedLength
                    });
                }
            }
            return chunkQueue;
        }
    }
}