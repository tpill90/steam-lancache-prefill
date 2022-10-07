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

        private PrefillSummaryResult _prefillSummaryResult;

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
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            _manifestHandler = new ManifestHandler(_ansiConsole, _cdnPool, _steam3, downloadArgs);
            _depotHandler = new DepotHandler(_steam3, _appInfoHandler);
        }

        /// <summary>
        /// Logs the user into the Steam network, and retrieves available CDN servers and account licenses.
        ///
        /// Required to be called first before using SteamManager class.
        /// </summary>
        public async Task InitializeAsync()
        {
            var timer = Stopwatch.StartNew();
            _ansiConsole.LogMarkupLine("Starting login!");

            await _steam3.LoginToSteamAsync();
            _steam3.WaitForLicenseCallback();

            FileLogger.Log(FileLogger.LogLevel.DEBUG, $"Steam session initialization complete after {timer.Elapsed}");
#if DEBUG
            _ansiConsole.LogMarkupLine("Steam session initialization complete!", timer.Elapsed);
#else
            _ansiConsole.LogMarkupLine("Steam session initialization complete!");
#endif

        }

        public void Shutdown()
        {
            _steam3.Disconnect();
        }

        public async Task DownloadMultipleAppsAsync(bool downloadAllOwnedGames, bool prefillRecentGames, int? prefillPopularGames, List<uint> manualIds)
        {
            _prefillSummaryResult = new PrefillSummaryResult();

            var appIdsToDownload = LoadPreviouslySelectedApps();
            appIdsToDownload.AddRange(manualIds);
            if (downloadAllOwnedGames)
            {
                appIdsToDownload.AddRange(_steam3.OwnedAppIds);
            }
            if (prefillRecentGames)
            {
                var recentGames = await _appInfoHandler.GetRecentlyPlayedGamesAsync();
                appIdsToDownload.AddRange(recentGames.Select(e => (uint)e.appid));
            }
            if (prefillPopularGames != null)
            {
                var popularGames = (await SteamSpy.TopGamesLast2WeeksAsync(_ansiConsole))
                                    .Take(prefillPopularGames.Value)
                                    .Select(e => e.appid);
                appIdsToDownload.AddRange(popularGames);
            }

            var distinctAppIds = appIdsToDownload.Distinct().ToList();
            await _appInfoHandler.RetrieveAppMetadataAsync(distinctAppIds);

            // Whitespace divider
            _ansiConsole.WriteLine();

            var availableGames = await _appInfoHandler.GetGamesByIdAsync(distinctAppIds);
            foreach (var app in availableGames)
            {
                try
                {
                    await DownloadSingleAppAsync(app.AppId);
                }
                catch (Exception e) when (e is LancacheNotFoundException || e is UserCancelledException || e is InfiniteLoopException)
                {
                    // We'll want to bomb out the entire process for these exceptions, as they mean we can't prefill any apps at all
                    FileLogger.Log(FileLogger.LogLevel.FATAL, e.Message);
                    throw;
                }
                catch (Exception e)
                {
                    // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                    FileLogger.Log(FileLogger.LogLevel.ERROR, $"Unexpected download error : {e.Message}");
                    _ansiConsole.MarkupLine(Red($"   Unexpected download error : {e.Message}"));
                    _ansiConsole.MarkupLine("");
                    _prefillSummaryResult.FailedApps++;
                }
            }
            await PrintUnownedAppsAsync(distinctAppIds);

            FileLogger.Log(FileLogger.LogLevel.INFO, "Prefill complete");
            _ansiConsole.LogMarkupLine("Prefill complete!");
            _prefillSummaryResult.RenderSummaryTable(_ansiConsole, availableGames.Count);
        }

        private async Task DownloadSingleAppAsync(uint appId)
        {
            AppInfo appInfo = await _appInfoHandler.GetAppInfoAsync(appId);
            
            // Filter depots based on specified lang/os/architecture/etc
            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(_downloadArgs, appInfo.Depots);
            if (!filteredDepots.Any())
            {
                //TODO add to summary output?
                _ansiConsole.LogMarkupLine($"Starting {Cyan(appInfo)}  {LightYellow("No depots to download.  Current arguments filtered all depots")}");
                return;
            }

            await _depotHandler.BuildLinkedDepotInfoAsync(filteredDepots);

            // We will want to re-download the entire app, if any of the depots have been updated
            if (_downloadArgs.Force == false && _depotHandler.AppIsUpToDate(filteredDepots))
            {
                _prefillSummaryResult.AlreadyUpToDate++;
                if (!AppConfig.VerboseLogs)
                {
                    return;
                }

                _ansiConsole.LogMarkupLine($"Starting {Cyan(appInfo)}  {Green("  Up to date!")}");
                return;
            }

            _ansiConsole.LogMarkupLine($"Starting {Cyan(appInfo)}");

            await _cdnPool.PopulateAvailableServersAsync(_steam3._cellId);

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

            var downloadSuccessful = await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue, _downloadArgs);
            if (downloadSuccessful)
            {
                _depotHandler.MarkDownloadAsSuccessful(filteredDepots);
                _prefillSummaryResult.Updated++;
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
            int chunkNum = 0;

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
                    chunkQueue.Add(new QueuedRequest(depotManifest, chunk, chunkNum++));
                }
            }
            return chunkQueue;
        }

        public void SetAppsAsSelected(List<AppInfo> userSelected)
        {
            List<uint> selectedAppIds = userSelected
                                        .Select(e => e.AppId)
                                        .ToList();
            File.WriteAllText(AppConfig.UserSelectedAppsPath, JsonSerializer.Serialize(selectedAppIds, SerializationContext.Default.ListUInt32));

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

        public async Task<List<AppInfo>> GetAllAvailableGamesAsync()
        {
            var ownedGameIds = _steam3.OwnedAppIds.ToList();

            // Loading app metadata from steam, skipping related DLC apps
            await _appInfoHandler.RetrieveAppMetadataAsync(ownedGameIds, loadDlcApps: false, loadRecentlyPlayed: true);
            var availableGames = await _appInfoHandler.GetGamesByIdAsync(ownedGameIds);
            return availableGames;
        }

        private async Task PrintUnownedAppsAsync(List<uint> distinctAppIds)
        {
            // Write out any apps that can't be downloaded as a warning message, so users can know that they were skipped
            AppInfo[] unownedApps = await Task.WhenAll(distinctAppIds.Where(e => !_steam3.AccountHasAppAccess(e))
                                                                      .Select(e => _appInfoHandler.GetAppInfoAsync(e)));
            _prefillSummaryResult.UnownedAppsSkipped = unownedApps.Length;


            if (!unownedApps.Any())
            {
                return;
            }

            var table = new Table { Border = TableBorder.MinimalHeavyHead };
            // Header
            table.AddColumn(new TableColumn(White("App")));

            // Rows
            foreach (var app in unownedApps.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
            {
                table.AddRow($"[link=https://store.steampowered.com/app/{app.AppId}]🔗[/] {White(app.Name)}");
            }

            _ansiConsole.MarkupLine("");
            _ansiConsole.MarkupLine(LightYellow($" Warning!  Found {Magenta(unownedApps.Length)} unowned apps!  They will be excluded from this prefill run..."));
            _ansiConsole.Write(table);
        }

        public void Dispose()
        {
            _downloadHandler.Dispose();
            _steam3.Dispose();
        }
    }
}