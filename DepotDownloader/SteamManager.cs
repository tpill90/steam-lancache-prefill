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
    //TODO I don't think this needs to be static
    //TODO needs to be reworked to support multiple apps at the same time
    //TODO document
    public class SteamManager
    {
        private readonly IAnsiConsole _ansiConsole;

        //TODO remove static
        public static AppConfig Config = new AppConfig();

		//TODO make private
        private Steam3Session _steam3;
        private Credentials _steam3Credentials;
        private CdnPool _cdnPool;
        private DownloadHandler _downloadHandler;
        private ManifestHandler _manifestHandler;
        private DepotHandler _depotHandler;

        public SteamManager(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
            // Create required folders
            Directory.CreateDirectory(AppConfig.ConfigDir);
            Directory.CreateDirectory(AppConfig.ManifestCacheDir);
        }

        // TODO comment
        // TODO look into persisting session so that this is faster
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

            // Initializing our various classes now that Steam is connected
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            _manifestHandler = new ManifestHandler(_ansiConsole, _cdnPool, _steam3);
            _depotHandler = new DepotHandler(_ansiConsole, _steam3);

            // Loading available licenses(games) for the current user
            _steam3.LoadAccountLicenses();

            _ansiConsole.LogMarkupLine("Initialization complete...", timer.Elapsed);
            _ansiConsole.WriteLine();
        }
        
        //TODO wrap in a spectre status?
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

        //TODO document
        public async Task BulkLoadAppInfos(List<uint> appIds)
        {
            await _steam3.BulkLoadAppInfos(appIds);
        }

        public async Task DownloadAppAsync(DownloadArguments downloadArgs)
        {
            _steam3.ThrowIfNotConnected();

            AppInfoShim appInfo = await _steam3.GetAppInfo(downloadArgs.AppId);
            _ansiConsole.LogMarkupLine($"Starting {Cyan(appInfo.Common.Name)}");

            //TODO this doesn't seem to be working correctly for games I don't own
            if (!_steam3.AccountHasAppAccess(appInfo.AppId))
            {
                //TODO handle this better
                throw new ContentDownloaderException($"App {appInfo.AppId} ({appInfo.Common.Name}) is not available from this account.");
            }

            // Get all depots, and filter them down based on lang/os/architecture/etc
            List<DepotInfo> filteredDepots = _depotHandler.FilterDepotsToDownload(downloadArgs, appInfo.Depots);
            await _depotHandler.BuildLinkedDepotInfo(filteredDepots, appInfo);

            // Get the full file list for each depot, and queue up the required chunks
            var chunkDownloadQueue = await BuildChunkDownloadQueue(filteredDepots);

            // Finally run the queued downloads
            await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue);

            //TODO total download size is wrong
            var totalBytes = ByteSize.FromBytes(chunkDownloadQueue.Sum(e => e.chunk.CompressedLength));
            _ansiConsole.LogMarkupLine($"Total downloaded: {Magenta(totalBytes.ToString())} from {Yellow(filteredDepots.Count)} depots");
            _ansiConsole.WriteLine();
        }

        //TODO document
        //TODO cleanup
        //TODO consider parallelizing this for speed
        //TODO implement logic that skips depots if they have previously been downloaded
        private async Task<List<QueuedRequest>> BuildChunkDownloadQueue(List<DepotInfo> depots)
        {
            var chunkQueue = new List<QueuedRequest>();
            var depotManifests = new ConcurrentBag<ProtoManifest>();

            var timer = Stopwatch.StartNew();

            // Fetch all the manifests for each depot in parallel, as individually they can take a long time, 
            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Fetching depot manifests...", async _ =>
            {
                await Parallel.ForEachAsync(depots, new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (depot, _) =>
                {
                    var manifest = await _manifestHandler.GetManifestFile(depot);
                    depotManifests.Add(manifest);
                });
            });

            // Queueing up chunks for each depot
            foreach (var depotManifest in depotManifests)
            {
                // A depot can be made up of multiple files
                foreach (var file in depotManifest.Files)
                {
                    // A file larger than 1MB will need to be downloaded in multiple chunks
                    foreach (var chunk in file.Chunks)
                    {
                        chunkQueue.Add(new QueuedRequest
                        {
                            DepotId = depotManifest.DepotId,
                            chunk = chunk
                        });
                    }
                }
            }
            _ansiConsole.LogMarkupLine("Built chunk download queue", timer.Elapsed);
            return chunkQueue;
        }
    }
}
