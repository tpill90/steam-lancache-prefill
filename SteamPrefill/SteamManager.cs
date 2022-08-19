using LancachePrefill.Common.Exceptions;

namespace SteamPrefill
{
    public sealed class SteamManager : IDisposable
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly DownloadArguments _downloadArgs;

        private readonly Steam3Session _steam3;
        private readonly CdnPool _cdnPool;

        private readonly DownloadHandler _downloadHandler;
        private readonly ManifestHandler _manifestHandler;
        private readonly DepotHandler _depotHandler;
        private readonly AppInfoHandler _appInfoHandler;

        public SteamManager(IAnsiConsole ansiConsole, DownloadArguments downloadArgs)
        {
            _ansiConsole = ansiConsole;
            _downloadArgs = downloadArgs;

            #if DEBUG

            if (AppConfig.EnableSteamKitDebugLogs)
            {
                DebugLog.AddListener(new SteamKitDebugListener(_ansiConsole));
                DebugLog.Enabled = true;
            }

            #endif
            
            _steam3 = new Steam3Session(_ansiConsole);
            _cdnPool = new CdnPool(_ansiConsole, _steam3);
            _appInfoHandler = new AppInfoHandler(_ansiConsole, _steam3);
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool, _downloadArgs);
            _manifestHandler = new ManifestHandler(_ansiConsole, _cdnPool, _steam3);
            _depotHandler = new DepotHandler(_steam3, _appInfoHandler);
        }

        /// <summary>
        /// Logs the user into the Steam network, and retrieves available CDN servers and account licenses.
        ///
        /// Required to be called first before using SteamManager class.
        /// </summary>
        public void Initialize()
        {
            _ansiConsole.LogMarkupLine("Starting login!");

            _steam3.LoginToSteam();
            _steam3.WaitForLicenseCallback();

            _ansiConsole.LogMarkupLine("Steam session initialization complete!");
        }

        public void Shutdown()
        {
            _steam3.Disconnect();
        }

        public async Task DownloadMultipleAppsAsync(bool downloadAllOwnedGames, List<uint> manualIds)
        {
            var timer = Stopwatch.StartNew();

            var appIdsToDownload = LoadPreviouslySelectedApps();
            appIdsToDownload.AddRange(manualIds);
            if (downloadAllOwnedGames)
            {
                appIdsToDownload.AddRange(_steam3.OwnedAppIds);
            }

            var distinctAppIds = appIdsToDownload.Distinct().OrderBy(e => e).ToList();

            await _appInfoHandler.RetrieveAppMetadataAsync(distinctAppIds);

            // Now we will be able to determine which apps can't be downloaded
            var availableGames = await _appInfoHandler.GetAvailableGamesAsync(distinctAppIds);

            // Whitespace divider
            _ansiConsole.WriteLine();

            foreach (var app in availableGames)
            {
                try
                {
                    await DownloadSingleAppAsync(app.AppId);
                }
                catch (LancacheNotFoundException e)
                {
                    throw e;
                }
                catch (UserCancelledException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                    _ansiConsole.MarkupLine(Red($"   Unexpected download error : {e.Message}"));
                    _ansiConsole.MarkupLine("");
                }
            }

            _ansiConsole.MarkupLine("");
            _ansiConsole.LogMarkupLine($"Prefill complete! Prefilled {Magenta(availableGames.Count)} apps in {LightYellow(timer.FormatElapsedString())}");
        }

        private async Task DownloadSingleAppAsync(uint appId)
        {
            AppInfo appInfo = await _appInfoHandler.GetAppInfoAsync(appId);
            _ansiConsole.LogMarkup($"Starting {Cyan(appInfo)}");

            // Filter depots based on specified lang/os/architecture/etc
            var filteredDepots = _depotHandler.FilterDepotsToDownload(_downloadArgs, appInfo.Depots);
            if (!filteredDepots.Any())
            {
                _ansiConsole.MarkupLine(LightYellow("  No depots to download.  Current arguments filtered all depots"));
                return;
            }

            await _depotHandler.BuildLinkedDepotInfoAsync(filteredDepots);

            // We will want to re-download the entire app, if any of the depots have been updated
            if (_downloadArgs.Force == false && _depotHandler.AppIsUpToDate(filteredDepots))
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
            _ansiConsole.Markup($" from {LightYellow(chunkDownloadQueue.Count)} chunks");
#endif
            _ansiConsole.MarkupLine("");

            var downloadSuccessful = await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue);
            if (downloadSuccessful)
            {
                _depotHandler.MarkDownloadAsSuccessful(filteredDepots);
            }
            downloadTimer.Stop();

            // Logging some metrics about the download
            _ansiConsole.LogMarkupLine($"Finished in {LightYellow(downloadTimer.FormatElapsedString())} - {Magenta(totalBytes.ToAverageString(downloadTimer))}");
            _ansiConsole.WriteLine();
        }
        
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

        public async Task<List<AppInfo>> GetGames()
        {
            var allApps = _steam3.OwnedAppIds.ToList();

            // Need to load the latest app information from steam, so that we have an updated list of all owned games
            await _appInfoHandler.RetrieveAppMetadataAsync(allApps);
            var availableGames = _appInfoHandler.GetAllAvailableGames();
            return availableGames;
        }

        //TODO is there any way to possibly speed this up, without having to query steam?
        public async Task SelectAppsAsync()
        {
            var allApps = _steam3.OwnedAppIds.ToList();

            // Need to load the latest app information from steam, so that we have an updated list of all owned games
            await _appInfoHandler.RetrieveAppMetadataAsync(allApps);
            var availableGames = _appInfoHandler.GetAllAvailableGames();

            // Whitespace divider
            _ansiConsole.WriteLine();
            _ansiConsole.Write(new Rule());

            var multiSelect = new MultiSelectionPrompt<AppInfo>()
                              .Title(Underline(White("Select apps to prefill...")))
                              .NotRequired()
                              .PageSize(45)
                              .MoreChoicesText(Grey("(Use ↑/↓ to navigate.  Page Up/Page Down skips pages)"))
                              .InstructionsText(Grey($"(Press {Blue("<space>")} to toggle an app, {Green("<enter>")} to accept)"))
                              .AddChoices(availableGames);

            // Restoring previously selected items
            foreach (var id in LoadPreviouslySelectedApps())
            {
                var appInfo = availableGames.FirstOrDefault(e => e.AppId == id);
                if (appInfo != null)
                {
                    multiSelect.Select(appInfo);
                }
            }

            List<uint> selectedAppIds = _ansiConsole.Prompt(multiSelect)
                                                    .Select(e => e.AppId)
                                                    .ToList();
            await File.WriteAllTextAsync(AppConfig.UserSelectedAppsPath, JsonSerializer.Serialize(selectedAppIds, SerializationContext.Default.ListUInt32));

            _ansiConsole.MarkupLine($"Selected {Magenta(selectedAppIds.Count)} apps to prefill!  ");
        }

        public List<uint> LoadPreviouslySelectedApps()
        {
            if (File.Exists(AppConfig.UserSelectedAppsPath))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(AppConfig.UserSelectedAppsPath), SerializationContext.Default.ListUInt32);
            }
            return new List<uint>();
        }

        public void Dispose()
        {
            _downloadHandler.Dispose();
            _steam3.Dispose();
        }
    }
}