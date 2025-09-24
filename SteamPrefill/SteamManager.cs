namespace SteamPrefill
{
    public sealed class SteamManager : IDisposable
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly DownloadArguments _downloadArgs;

        private readonly Steam3Session _steam3;
        private readonly CdnPool _cdnPool;

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
            _steam3.Dispose();
        }

        #endregion

        #region Prefill


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

            downloadTimer.Stop();
        }

        #endregion


        //TODO consider breaking this out into its own class

        #region Benchmarking

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

        public async Task<List<AppInfo>> GetAllAvailableAppsAsync()
        {
            var ownedGameIds = _steam3.LicenseManager.AllOwnedAppIds;

            // Loading app metadata from steam, skipping related DLC apps
            await _appInfoHandler.RetrieveAppMetadataAsync(ownedGameIds, getRecentlyPlayedMetadata: true);
            var availableGames = await _appInfoHandler.GetAvailableGamesByIdAsync(ownedGameIds);

            return availableGames;
        }

    }
}