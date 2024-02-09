namespace SteamPrefill
{
    public sealed class SteamManager : IDisposable
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly DownloadArguments _downloadArgs;

        private readonly Steam3Session _steam3;
        private readonly CdnPool _cdnPool;

        private readonly DownloadHandler _downloadHandler;
        private readonly DepotHandler _depotHandler;
        private readonly AppInfoHandler _appInfoHandler;

        private readonly PrefillSummaryResult _prefillSummaryResult = new PrefillSummaryResult();

        public SteamManager(IAnsiConsole ansiConsole, DownloadArguments downloadArgs)
        {
            _ansiConsole = ansiConsole;
            _downloadArgs = downloadArgs;

            if (AppConfig.EnableSteamKitDebugLogs)
            {
                DebugLog.AddListener(new SteamKitDebugListener(_ansiConsole));
                DebugLog.Enabled = true;
            }

            _steam3 = new Steam3Session(_ansiConsole);
            _cdnPool = new CdnPool(_ansiConsole, _steam3);
            _appInfoHandler = new AppInfoHandler(_ansiConsole, _steam3, _steam3.LicenseManager);
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            _depotHandler = new DepotHandler(_ansiConsole, _steam3, _appInfoHandler, _cdnPool, downloadArgs);
        }

        #region Startup + Shutdown

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

            _ansiConsole.LogMarkupLine("Steam session initialization complete!", timer);
            // White spacing + a horizontal rule to delineate that initialization has completed
            _ansiConsole.WriteLine();
            _ansiConsole.Write(new Rule());

        }

        public void Shutdown()
        {
            _steam3.Disconnect();
        }

        public void Dispose()
        {
            _downloadHandler.Dispose();
            _steam3.Dispose();
        }

        #endregion

        #region Prefill

        public async Task DownloadMultipleAppsAsync(bool downloadAllOwnedGames, bool prefillRecentGames, int? prefillPopularGames)
        {
            // Building out full list of AppIds to use.
            var appIdsToDownload = LoadPreviouslySelectedApps();
            if (downloadAllOwnedGames)
            {
                appIdsToDownload.AddRange(_steam3.LicenseManager.AllOwnedAppIds);
            }
            if (prefillRecentGames)
            {
                var recentGames = await _appInfoHandler.GetRecentlyPlayedGamesAsync();
                appIdsToDownload.AddRange(recentGames.Select(e => (uint)e.appid));
            }
            if (prefillPopularGames != null)
            {
                var popularGames = (await SteamChartsService.MostPlayedByDailyPlayersAsync(_ansiConsole))
                                    .Take(prefillPopularGames.Value)
                                    .Select(e => e.AppId);
                appIdsToDownload.AddRange(popularGames);
            }

            // AppIds can potentially be added twice when building out the full list of ids
            var distinctAppIds = appIdsToDownload.Distinct().ToList();
            await _appInfoHandler.RetrieveAppMetadataAsync(distinctAppIds);

            // Whitespace divider
            _ansiConsole.WriteLine();

            var availableGames = await _appInfoHandler.GetAvailableGamesByIdAsync(distinctAppIds);
            foreach (var app in availableGames)
            {
                try
                {
                    await DownloadSingleAppAsync(app.AppId);
                }
                catch (Exception e) when (e is LancacheNotFoundException || e is InfiniteLoopException)
                {
                    // We'll want to bomb out the entire process for these exceptions, as they mean we can't prefill any apps at all
                    throw;
                }
                catch (Exception e)
                {
                    // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                    _ansiConsole.LogMarkupLine(Red($"Unexpected download error : {e.Message}  Skipping app..."));
                    _ansiConsole.MarkupLine("");
                    FileLogger.LogException(e);

                    _prefillSummaryResult.FailedApps++;
                }
            }
            await PrintUnownedAppsAsync(distinctAppIds);

            _ansiConsole.LogMarkupLine("Prefill complete!");
            _prefillSummaryResult.RenderSummaryTable(_ansiConsole);
        }

        private async Task DownloadSingleAppAsync(uint appId)
        {
            AppInfo appInfo = await _appInfoHandler.GetAppInfoAsync(appId);

            // Filter depots based on specified language/OS/cpu architecture/etc
            var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(_downloadArgs, appInfo.Depots);
            if (filteredDepots.Empty())
            {
                _ansiConsole.LogMarkupLine($"Starting {Cyan(appInfo)}  {LightYellow("No depots to download.  Current arguments filtered all depots")}");
                return;
            }

            await _depotHandler.BuildLinkedDepotInfoAsync(filteredDepots);

            // We will want to re-download the entire app, if any of the depots have been updated
            if (_downloadArgs.Force == false && _depotHandler.AppIsUpToDate(filteredDepots))
            {
                _prefillSummaryResult.AlreadyUpToDate++;
                return;
            }

            _ansiConsole.LogMarkupLine($"Starting {Cyan(appInfo)}");

            await _cdnPool.PopulateAvailableServersAsync();

            // Get the full file list for each depot, and queue up the required chunks
            List<QueuedRequest> chunkDownloadQueue = null;
            await _ansiConsole.StatusSpinner().StartAsync("Fetching depot manifests...", async _ =>
            {
                chunkDownloadQueue = await _depotHandler.BuildChunkDownloadQueueAsync(filteredDepots);
            });

            // Finally run the queued downloads
            var downloadTimer = Stopwatch.StartNew();
            var totalBytes = ByteSize.FromBytes(chunkDownloadQueue.Sum(e => e.CompressedLength));
            _prefillSummaryResult.TotalBytesTransferred += totalBytes;

            _ansiConsole.LogMarkupVerbose($"Downloading {Magenta(totalBytes.ToDecimalString())} from {LightYellow(chunkDownloadQueue.Count)} chunks");

            if (AppConfig.SkipDownloads)
            {
                _ansiConsole.MarkupLine("");
                return;
            }

            var downloadSuccessful = await _downloadHandler.DownloadQueuedChunksAsync(chunkDownloadQueue, _downloadArgs);
            if (downloadSuccessful)
            {
                _depotHandler.MarkDownloadAsSuccessful(filteredDepots);
                _prefillSummaryResult.Updated++;
            }
            else
            {
                _prefillSummaryResult.FailedApps++;
            }
            downloadTimer.Stop();

            // Logging some metrics about the download
            _ansiConsole.LogMarkupLine($"Finished in {LightYellow(downloadTimer.FormatElapsedString())} - {Magenta(totalBytes.CalculateBitrate(downloadTimer))}");
            _ansiConsole.WriteLine();
        }

        #endregion

        #region Select Apps

        public void SetAppsAsSelected(List<TuiAppInfo> tuiAppModels)
        {
            List<uint> selectedAppIds = tuiAppModels.Where(e => e.IsSelected)
                                                    .Select(e => UInt32.Parse(e.AppId))
                                                    .ToList();
            File.WriteAllText(AppConfig.UserSelectedAppsPath, JsonSerializer.Serialize(selectedAppIds, SerializationContext.Default.ListUInt32));

            _ansiConsole.LogMarkupLine($"Selected {Magenta(selectedAppIds.Count)} apps to prefill!  ");
        }

        public List<uint> LoadPreviouslySelectedApps()
        {
            if (File.Exists(AppConfig.UserSelectedAppsPath))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(AppConfig.UserSelectedAppsPath), SerializationContext.Default.ListUInt32);
            }
            return new List<uint>();
        }

        #endregion

        public async Task<List<AppInfo>> GetAllAvailableAppsAsync()
        {
            var ownedGameIds = _steam3.LicenseManager.AllOwnedAppIds;

            // Loading app metadata from steam, skipping related DLC apps
            await _appInfoHandler.RetrieveAppMetadataAsync(ownedGameIds, loadDlcApps: false, loadRecentlyPlayed: true);
            var availableGames = await _appInfoHandler.GetAvailableGamesByIdAsync(ownedGameIds);

            return availableGames;
        }

        private async Task PrintUnownedAppsAsync(List<uint> distinctAppIds)
        {
            // Write out any apps that can't be downloaded as a warning message, so users can know that they were skipped
            AppInfo[] unownedApps = await Task.WhenAll(distinctAppIds.Where(e => !_steam3.LicenseManager.AccountHasAppAccess(e))
                                                                      .Select(e => _appInfoHandler.GetAppInfoAsync(e)));
            _prefillSummaryResult.UnownedAppsSkipped = unownedApps.Length;


            if (unownedApps.Empty())
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

        //TODO consider breaking this out into its own class
        #region Benchmarking

        public async Task SetupBenchmarkAsync(List<uint> appIds, bool useAllOwnedGames, bool useSelectedApps)
        {
            _ansiConsole.WriteLine();
            _ansiConsole.LogMarkupLine("Building benchmark workload file...");

            // Building out list of apps to benchmark
            if (useSelectedApps)
            {
                appIds.AddRange(LoadPreviouslySelectedApps());
            }
            if (useAllOwnedGames)
            {
                appIds.AddRange(_steam3.LicenseManager.AllOwnedAppIds);
            }
            appIds = appIds.Distinct().ToList();

            // Preloading as much metadata as possible
            await _appInfoHandler.RetrieveAppMetadataAsync(appIds);
            await PrintUnownedAppsAsync(appIds);

            // Building out the combined workload file
            BenchmarkWorkload benchmarkWorkload = await BuildBenchmarkWorkloadAsync(appIds);

            // Saving results to disk
            benchmarkWorkload.SaveToFile(AppConfig.BenchmarkWorkloadPath);

            // No need to display the summary table if the benchmark wasn't built.  This can happen if the user passed in an unowned appid with no other appids
            if (benchmarkWorkload.AllQueuedRequests.Empty())
            {
                _ansiConsole.LogMarkupError("Benchmark file not built!  All apps were unowned and could not be included!");
                return;
            }

            // Writing stats
            benchmarkWorkload.PrintSummary(_ansiConsole);

            var fileSize = ByteSize.FromBytes(new FileInfo(AppConfig.BenchmarkWorkloadPath).Length);
            _ansiConsole.Write(new Rule());
            _ansiConsole.LogMarkupLine("Completed build of workload file...");
            _ansiConsole.LogMarkupLine($"Resulting file size : {MediumPurple(fileSize.ToBinaryString())}");
        }

        private async Task<BenchmarkWorkload> BuildBenchmarkWorkloadAsync(List<uint> appIds)
        {
            await _cdnPool.PopulateAvailableServersAsync();

            var queuedApps = new ConcurrentBag<AppQueuedRequests>();
            await _ansiConsole.CreateSpectreProgress(TransferSpeedUnit.Bytes, displayTransferRate: false).StartAsync(async ctx =>
            {
                var gamesToUse = await _appInfoHandler.GetAvailableGamesByIdAsync(appIds);
                var overallProgressTask = ctx.AddTask("Processing games..".PadLeft(30), new ProgressTaskSettings { MaxValue = gamesToUse.Count });

                await Parallel.ForEachAsync(gamesToUse, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (appInfo, _) =>
                {
                    var individualProgressTask = ctx.AddTask($"{Cyan(appInfo.Name.Truncate(30).PadLeft(30))}");
                    individualProgressTask.IsIndeterminate = true;

                    try
                    {
                        var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(_downloadArgs, appInfo.Depots);
                        await _depotHandler.BuildLinkedDepotInfoAsync(filteredDepots);
                        if (filteredDepots.Empty())
                        {
                            _ansiConsole.LogMarkupLine($"{Cyan(appInfo)} - {LightYellow("No depots to download.  Current arguments filtered all depots")}");
                            return;
                        }

                        // Get the full file list for each depot, and queue up the required chunks
                        var allChunksForApp = await _depotHandler.BuildChunkDownloadQueueAsync(filteredDepots);
                        var appFileListing = new AppQueuedRequests(appInfo.Name, appInfo.AppId, allChunksForApp);
                        queuedApps.Add(appFileListing);
                    }
                    catch (Exception e) when (e is LancacheNotFoundException || e is InfiniteLoopException)
                    {
                        // Bomb out the whole process, since these are completely unrecoverable
                        throw;
                    }
                    catch (Exception e)
                    {
                        // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                        _ansiConsole.MarkupLine(Red($"   Unexpected error : {e.Message}"));
                        _ansiConsole.MarkupLine("");
                    }

                    overallProgressTask.Increment(1);
                    individualProgressTask.StopTask();
                });
            });
            return new BenchmarkWorkload(queuedApps, _cdnPool.AvailableServerEndpoints);
        }

        #endregion

        #region Status

        public async Task CurrentlyDownloadedAsync(SortOrder sortOrder, string sortColumn)
        {
            await _cdnPool.PopulateAvailableServersAsync();

            // Pre-Load all selected apps and their manifests
            List<uint> appIds = LoadPreviouslySelectedApps();
            await _appInfoHandler.RetrieveAppMetadataAsync(appIds);

            ByteSize totalSize = new ByteSize();
            Dictionary<string, ByteSize> index = new Dictionary<string, ByteSize>();

            var timer = Stopwatch.StartNew();
            _ansiConsole.LogMarkupLine("Loading Manifests");
            foreach (uint appId in appIds)
            {
                AppInfo appInfo = await _appInfoHandler.GetAppInfoAsync(appId);

                var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(_downloadArgs, appInfo.Depots);
                await _depotHandler.BuildLinkedDepotInfoAsync(filteredDepots);

                var allChunksForApp = await _depotHandler.BuildChunkDownloadQueueAsync(filteredDepots);
                var size = ByteSize.FromBytes(allChunksForApp.Sum(e => e.CompressedLength));
                totalSize += size;

                index.Add(appInfo.Name, size);
            }
            _ansiConsole.LogMarkupLine("Manifests Loaded", timer);

            var table = new Table { Border = TableBorder.MinimalHeavyHead };
            table.AddColumns(new TableColumn("App"), new TableColumn("Size"));

            foreach (KeyValuePair<string, ByteSize> data in SortData(index, sortOrder, sortColumn))
            {
                string appName = data.Key;
                ByteSize size = data.Value;
                table.AddRow(appName, size.ToDecimalString());
            }

            table.AddEmptyRow();
            table.AddRow("Total Size", totalSize.ToDecimalString());

            _ansiConsole.Write(table);
        }

        private IOrderedEnumerable<KeyValuePair<string, ByteSize>> SortData(
            Dictionary<string, ByteSize> index,
            SortOrder sortOrder,
            string sortColumn)
        {
            if (sortOrder == SortOrder.Ascending)
            {
                if (sortColumn.Equals("app", StringComparison.OrdinalIgnoreCase))
                {
                    return index.OrderBy(o => o.Key);
                }
                else if (sortColumn.Equals("size", StringComparison.OrdinalIgnoreCase))
                {
                    return index.OrderBy(o => o.Value);
                }
            }
            else if (sortOrder == SortOrder.Descending)
            {
                if (sortColumn.Equals("app", StringComparison.OrdinalIgnoreCase))
                {
                    return index.OrderByDescending(o => o.Key);
                }
                else if (sortColumn.Equals("size", StringComparison.OrdinalIgnoreCase))
                {
                    return index.OrderByDescending(o => o.Value);
                }
            }
            return index.OrderBy(o => o.Key);
        }

        #endregion
    }
}