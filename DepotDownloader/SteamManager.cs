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
        public static DownloadConfig Config = new DownloadConfig();

        private Steam3Session _steam3;
        private Credentials _steam3Credentials;
        private CDNClientPool _cdnPool;
        private DownloadHandler _downloadHandler;

        public SteamManager(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
            // Create required folders
            Directory.CreateDirectory(DownloadConfig.ConfigDir);
            Directory.CreateDirectory(DownloadConfig.ManifestCacheDir);
        }

        // TODO comment
        // TODO look into persisting session so that this is faster
        // TODO need to test an account with steam guard
        public async Task Initialize(string username, string password)
        {
            var timer = Stopwatch.StartNew();

            // capture the supplied password in case we need to re-use it after checking the login key
            Config.SuppliedPassword = password;

            ConnectToSteam(username, password);

            // Loading cached data from previous runs
            _steam3.LoadCachedData();

            // Populating available CDN servers
            _cdnPool = new CDNClientPool(_steam3);
            await _cdnPool.PopulateAvailableServers();
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);

            // Loading available licenses(games) for the current user
            _steam3.LoadAccountLicenses();

            _ansiConsole.LogMarkupLine("Initialization complete...", timer.Elapsed);
            _ansiConsole.WriteLine();
        }
        
        public void Shutdown()
        {
            _steam3.SerializeCachedData();
        }

        private void ConnectToSteam(string username, string password)
        {
            var timer = Stopwatch.StartNew();
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

            _ansiConsole.LogMarkupLine("Logged into steam", timer.Elapsed);
        }

        //TODO lookup the app name by id
        public async Task DownloadAppAsync(DownloadArguments downloadArgs)
        {
            if (_steam3 == null)
            {
                throw new Exception("Steam session not initialized!!");
            }

            AppInfoShim appInfo = _steam3.GetAppInfo(downloadArgs.AppId);
            DetermineIfUserHasAccess(appInfo);

            // Get all depots, and filter them down based on lang/os/architecture/etc
            List<DepotInfo> filteredDepots = DepotHandler.FilterDepotsToDownload(downloadArgs, appInfo.Depots, Config);
            var depotsToDownload = DepotHandler.GetDepotDownloadInfo(filteredDepots, _steam3, appInfo);

            // Get the full file list for each depot, and queue up the required chunks
            var manifests = await GetManifests(depotsToDownload);
            var chunkDownloadQueue = BuildChunkDownloadQueue(manifests);

            // Finally run the queued downloads
            await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue);
            //TODO total download is wrong
            var totalBytes = ByteSize.FromBytes(chunkDownloadQueue.Sum(e => e.chunk.UncompressedLength));
            _ansiConsole.LogMarkupLine($"Total downloaded: {Magenta(totalBytes.ToString())} from {Yellow(manifests.Count)} depots");
        }

        //TODO document
        private async Task<List<DepotFilesData>> GetManifests(List<DepotDownloadInfo> depots)
        {
            var depotsToDownload = new List<DepotFilesData>();
            var timer = Stopwatch.StartNew();

            // First, fetch all the manifests for each depot
            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Fetching", async ctx =>
            {
                foreach (var depot in depots)
                {
                    ctx.Status = $"Fetching depot files for {Yellow(depot.id)} - {Cyan(depot.contentName)}";

                    DepotFilesData depotFileData = await ManifestHandler.GetManifestFilesAsync(depot, _cdnPool, _steam3);
                    depotsToDownload.Add(depotFileData);
                }
                
            });
            _ansiConsole.LogMarkupLine("Got manifests", timer.Elapsed);
            return depotsToDownload;
        }

        //TODO document
        private ConcurrentBag<QueuedRequest> BuildChunkDownloadQueue(List<DepotFilesData> depotsToDownload)
        {
            var timer = Stopwatch.StartNew();
            var networkChunkQueue = new ConcurrentBag<QueuedRequest>();

            // Process each depot, and aggregate all chunks that need to be downloaded
            foreach (var depotFilesData in depotsToDownload)
            {
                // A depot can be made up of multiple files
                foreach(var file in depotFilesData.filteredFiles)
                {
                    // A file larger than 1MB will need to be downloaded in multiple chunks
                    foreach (var chunk in file.Chunks)
                    {
                        networkChunkQueue.Add(new QueuedRequest
                        {
                            chunk = chunk,
                            depotDownloadInfo = depotFilesData.depotDownloadInfo
                        });
                    }
                }
            }
            _ansiConsole.LogMarkupLine("Build chunk queue", timer.Elapsed);
            return networkChunkQueue;
        }

        //TODO comment
        //TODO determine how to handle user adding new games to account
        private void DetermineIfUserHasAccess(AppInfoShim app)
        {
            var timer = Stopwatch.StartNew();
            //TODO this doesn't seem to be working correctly for games I don't own
            if (DepotHandler.AccountHasAccess(app.AppId, _steam3))
            {
                _ansiConsole.LogMarkupLine("Determined user app access ", timer.Elapsed);
                return;
            }

            throw new ContentDownloaderException($"App {app.AppId} ({app.Common.Name}) is not available from this account.");
        }
    }
}