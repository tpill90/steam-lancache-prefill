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

            _steam3 = new Steam3Session(_ansiConsole);
            _cdnPool = new CdnPool(_ansiConsole, _steam3);
            _appInfoHandler = new AppInfoHandler(_ansiConsole, _steam3, _steam3.LicenseManager);
            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            _depotHandler = new DepotHandler(_ansiConsole, _steam3, _appInfoHandler, _cdnPool);
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

        /// <summary>
        /// Given a list of AppIds, determines which apps require updates, and downloads the required depots.  By default,
        /// it will always include apps chosen by the select-apps command.
        /// </summary>
        /// <param name="downloadAllOwnedGames">If set to true, all games owned by the user will be downloaded</param>
        /// <param name="prefillRecentGames">If set to true, games played in the last 2 weeks will be downloaded</param>
        /// <param name="prefillPopularGames">If set to a value > 0, the most popular N games will be downloaded</param>
        /// <param name="prefillRecentlyPurchasedGames">If set to true, games purchased in the last 2 weeks will be downloaded</param>
        public async Task DownloadMultipleAppsAsync(bool downloadAllOwnedGames, bool prefillRecentGames,
                                                    int? prefillPopularGames, bool prefillRecentlyPurchasedGames)
        {
            // Building out the list of AppIds to download
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
            if (prefillRecentlyPurchasedGames)
            {
                var recentApps = _steam3.LicenseManager.GetRecentlyPurchasedAppIds(30);
                appIdsToDownload.AddRange(recentApps);

                // Verbose logging for recently purchased games
                await _appInfoHandler.RetrieveAppMetadataAsync(recentApps);
                _ansiConsole.LogMarkupVerbose("[bold yellow]Recently purchased games (last 2 weeks):[/]");
                foreach (var appId in recentApps)
                {
                    var purchaseDate = _steam3.LicenseManager.GetPurchaseDateForApp(appId);
                    var appInfo = await _appInfoHandler.GetAppInfoAsync(appId);
                    _ansiConsole.LogMarkupVerbose($"  {Green(appInfo.Name).PadRight(35)} - Purchased: {LightYellow(purchaseDate.ToLocalTime().ToString("yyyy-MM-dd"))}");
                }
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
                    await DownloadSingleAppAsync(app);
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

        private async Task DownloadSingleAppAsync(AppInfo appInfo)
        {
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
            await _ansiConsole.StatusSpinner().StartAsync("Fetching depot manifests...", async _ => { chunkDownloadQueue = await _depotHandler.BuildChunkDownloadQueueAsync(filteredDepots); });

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

                // Logging some metrics about the download
                _ansiConsole.LogMarkupLine($"Finished in {LightYellow(downloadTimer.FormatElapsedString())} - {Magenta(totalBytes.CalculateBitrate(downloadTimer))}");
                _ansiConsole.WriteLine();
            }
            else
            {
                _prefillSummaryResult.FailedApps++;
            }
            downloadTimer.Stop();
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
            if (!File.Exists(AppConfig.UserSelectedAppsPath))
            {
                return new List<uint>();
            }

            return JsonSerializer.Deserialize(File.ReadAllText(AppConfig.UserSelectedAppsPath), SerializationContext.Default.ListUInt32);
        }

        #endregion

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

            // White spacing + a horizontal rule to delineate that the command has completed
            _ansiConsole.WriteLine();
            _ansiConsole.Write(new Rule());

            // Writing stats
            benchmarkWorkload.PrintSummary(_ansiConsole);

            CheckBenchmarkWorkloadSize(benchmarkWorkload);

            _ansiConsole.Write(new Rule());
            _ansiConsole.LogMarkupLine("Completed build of workload file...");
        }

        //TODO document
        private static void CheckBenchmarkWorkloadSize(BenchmarkWorkload generatedWorkload)
        {
            // While technically you can run a benchmark from a client Linux machine, this scenario is probably pretty rare since most people
            // run SteamPrefill on the cache host.  Rather than add some more complexity by checking that the benchmark is definitely being created
            // on the cache host, we'll omit it and save some added complexity
            if (!System.OperatingSystem.IsLinux())
            {
                return;
            }

            var systemMemory = SystemMemoryMetrics.GetTotalSystemMemory();
            // If the user generated a workload that is larger than the system's total memory, then the benchmark will always be reading from disk.
            if (generatedWorkload.TotalDownloadSize > systemMemory)
            {
                return;
            }

            // Table setup
            var table = new Table
            {
                ShowHeaders = false,
                Border = TableBorder.Rounded,
                BorderStyle = new Style(SpectreColors.LightYellow)
            };
            table.AddColumn("");

            // Adding message rows
            var totalDownloadSize = generatedWorkload.TotalDownloadSize;
            table.AddRow(LightYellow($"{new string(' ', 40)}!!!!!! Warning !!!!!!"));
            table.AddEmptyRow();
            table.AddRow($"The generated workload size of {Magenta(totalDownloadSize.ToBinaryString())} " +
                         $"is smaller than the total system memory of {LightYellow(systemMemory.ToDecimalString())}.");
            table.AddRow("Linux will cache files that it reads in system memory to improve performance of frequently used files,");
            table.AddRow("however this benchmark is typically used to test disk IO performance.");
            table.AddRow("In order to guarantee that an accurate benchmark where files are only ever read from disk,");
            table.AddRow("the workload size should be larger than the system's memory.");
            table.AddEmptyRow();
            table.AddRow($"Please create a new benchmark workload that is larger than {LightYellow(systemMemory.ToDecimalString())}.");


            // Render the table to the console
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        //TODO this method is awfully complex, has lots and lots of nesting.  Makes it a bit hard to read at a glance
        private async Task<BenchmarkWorkload> BuildBenchmarkWorkloadAsync(List<uint> appIds)
        {
            await _cdnPool.PopulateAvailableServersAsync();

            var queuedApps = new ConcurrentBag<AppQueuedRequests>();
            await _ansiConsole.CreateSpectreProgress().StartAsync(async ctx =>
            {
                var gamesToUse = await _appInfoHandler.GetAvailableGamesByIdAsync(appIds);
                var overallProgressTask = ctx.AddTask("Processing apps..".PadLeft(30), new ProgressTaskSettings { MaxValue = gamesToUse.Count });

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

        //TODO comment
        public async Task PrintSelectedAppsStatsAsync(SortOrder sortOrder, SortColumn sortColumn)
        {
            _ansiConsole.WriteLine();
            _ansiConsole.LogMarkupLine("Building statistics for currently selected apps...");

            // Pre-Load all selected apps and their manifests
            List<uint> appIds = LoadPreviouslySelectedApps();
            await _appInfoHandler.RetrieveAppMetadataAsync(appIds);
            await _cdnPool.PopulateAvailableServersAsync();

            // Dictionary of app names + download sizes
            var index = new ConcurrentDictionary<string, ByteSize>();

            var timer = Stopwatch.StartNew();
            var availableGames = await _appInfoHandler.GetAvailableGamesByIdAsync(appIds);
            await _ansiConsole.CreateSpectreProgress().StartAsync(async ctx =>
            {
                var overallProgressTask = ctx.AddTask("Processing apps..".PadLeft(30), new ProgressTaskSettings { MaxValue = availableGames.Count });

                await Parallel.ForEachAsync(availableGames, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (app, _) =>
                {
                    var individualProgressTask = ctx.AddTask($"{Cyan(app.Name.Truncate(30).PadLeft(30))}");
                    individualProgressTask.IsIndeterminate = true;

                    var filteredDepots = await _depotHandler.FilterDepotsToDownloadAsync(_downloadArgs, app.Depots);
                    await _depotHandler.BuildLinkedDepotInfoAsync(filteredDepots);

                    var allChunksForApp = await _depotHandler.BuildChunkDownloadQueueAsync(filteredDepots);
                    var downloadSize = ByteSize.FromBytes(allChunksForApp.Sum(e => e.CompressedLength));

                    index.TryAdd(app.Name, downloadSize);

                    overallProgressTask.Increment(1);
                    individualProgressTask.StopTask();
                });
            });

            var totalDownloadSize = ByteSize.FromBytes(index.Values.Sum(e => e.Bytes));
            _ansiConsole.LogMarkupLine("Finished loading manifest metadata", timer);

            // White spacing + a horizontal rule to delineate that the command has completed
            _ansiConsole.WriteLine();
            _ansiConsole.Write(new Rule());

            // Printing out result table
            var table = new Table { Border = TableBorder.MinimalHeavyHead };
            // Header
            table.AddColumn(new TableColumn(Cyan("App")));
            table.AddColumn(new TableColumn(MediumPurple("Download Size")).RightAligned());

            foreach (KeyValuePair<string, ByteSize> data in SortData(index, sortOrder, sortColumn))
            {
                string appName = data.Key;
                ByteSize size = data.Value;
                table.AddRow(appName, size.ToDecimalString());
            }
            // Summary footer
            table.Columns[1].Footer = new Markup(Bold(White(totalDownloadSize.ToDecimalString())));
            _ansiConsole.Write(table);
            _ansiConsole.Write(new Rule());
        }

        //TODO rename
        private IOrderedEnumerable<KeyValuePair<string, ByteSize>> SortData(ConcurrentDictionary<string, ByteSize> index, SortOrder sortOrder, SortColumn sortColumn)
        {
            if (sortOrder == SortOrder.Ascending)
            {
                if (sortColumn == SortColumn.App)
                {
                    return index.OrderBy(o => o.Key);
                }
                return index.OrderBy(o => o.Value);
            }

            // Descending
            if (sortColumn == SortColumn.App)
            {
                return index.OrderByDescending(o => o.Key);
            }
            return index.OrderByDescending(o => o.Value);
        }

        #endregion

        public async Task<List<AppInfo>> GetAllAvailableAppsAsync()
        {
            var ownedGameIds = _steam3.LicenseManager.AllOwnedAppIds;

            // Loading app metadata from steam, skipping related DLC apps
            await _appInfoHandler.RetrieveAppMetadataAsync(ownedGameIds, getRecentlyPlayedMetadata: true);
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

    }
}