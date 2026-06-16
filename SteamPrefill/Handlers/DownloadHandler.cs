namespace SteamPrefill.Handlers
{
    public sealed class DownloadHandler : IDisposable
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly CdnPool _cdnPool;
        private readonly HttpClient _client;

        /// <summary>
        /// The URL/IP Address where the Lancache has been detected.
        /// </summary>
        private string _lancacheAddress;

        public DownloadHandler(IAnsiConsole ansiConsole, CdnPool cdnPool)
        {
            _ansiConsole = ansiConsole;
            _cdnPool = cdnPool;

            _client = new HttpClient();
            // Lancache requires this user agent in order to correctly identify and cache Valve's content servers
            _client.DefaultRequestHeaders.Add("User-Agent", "Valve/Steam HTTP Client 1.0");
        }

        public async Task InitializeAsync()
        {
            if (_lancacheAddress == null)
            {
                _lancacheAddress = await LancacheIpResolver.ResolveLancacheIpAsync(_ansiConsole, AppConfig.SteamTriggerDomain);
            }
        }

        /// <summary>
        /// Attempts to download all queued requests.  If all downloads are successful, will return true.
        /// In the case of any failed downloads, the failed downloads will be retried up to 3 times.  If the downloads fail 3 times, then
        /// false will be returned
        /// </summary>
        /// <returns>True if all downloads succeeded.  False if any downloads failed 3 times in a row.</returns>
        public async Task<bool> DownloadQueuedChunksAsync(List<QueuedRequest> queuedRequests, DownloadArguments downloadArgs)
        {
            await InitializeAsync();

            int retryCount = 0;
            var failedRequests = new ConcurrentBag<QueuedRequest>();
            // Tracks CDN servers that have returned a 403 for a given depot.  A 403 means that CDN does not cache that
            // depot, so we avoid re-using it for that depot (while still allowing it to serve other depots).  Persisted
            // across retries so blacklisted servers stay excluded.  depotId -> set of blacklisted server hosts.
            var depotServerBlacklist = new ConcurrentDictionary<uint, ConcurrentDictionary<string, byte>>();

            // Not every CDN caches every depot, so probe once per depot up front to find (and report) a server that
            // actually serves it.  This avoids a storm of failed requests being discovered during the bulk download.
            var depotServers = await ProbeDepotServersAsync(queuedRequests, depotServerBlacklist);

            await _ansiConsole.CreateSpectreProgress(downloadArgs.TransferSpeedUnit).StartAsync(async ctx =>
            {
                // Run the initial download
                failedRequests = await AttemptDownloadAsync(ctx, "Downloading..", queuedRequests, downloadArgs, depotServers: depotServers, depotServerBlacklist: depotServerBlacklist);

                // Handle any failed requests
                while (failedRequests.Any() && retryCount < 2)
                {
                    retryCount++;
                    failedRequests = await AttemptDownloadAsync(ctx, $"Retrying  {retryCount}..", failedRequests.ToList(), downloadArgs, forceRecache: true, depotServers: depotServers, depotServerBlacklist: depotServerBlacklist);
                }
            });

            // Handling final failed requests
            if (failedRequests.IsEmpty)
            {
                return true;
            }

            _ansiConsole.LogMarkupError($"Download failed! {LightYellow(failedRequests.Count)} requests failed unexpectedly, see {LightYellow("app.log")} for more details.");
            _ansiConsole.WriteLine();

            // Web requests frequently fail due to transient errors, so displaying all errors to the user is unnecessary or even confusing.
            // However, if a request fails repeatedly then there might be an underlying issue preventing success.
            // The number of failures could approach in the thousands or even more, so rather than spam the console
            // we will instead log them as a batch to app.log
            foreach (var failedRequest in failedRequests)
            {
                FileLogger.LogExceptionNoStackTrace($"Request /depot/{failedRequest.DepotId}/chunk/{failedRequest.ChunkId} failed", failedRequest.LastFailureReason);
            }
            return false;
        }

        //TODO I don't like the number of parameters here, should maybe rethink the way this is written.
        /// <summary>
        /// Attempts to download the specified requests.  Returns a list of any requests that have failed for any reason.
        /// </summary>
        /// <param name="forceRecache">When specified, will cause the cache to delete the existing cached data for a request, and re-download it again.</param>
        /// <returns>A list of failed requests</returns>
        public async Task<ConcurrentBag<QueuedRequest>> AttemptDownloadAsync(ProgressContext ctx, string taskTitle, List<QueuedRequest> requestsToDownload,
                                                                                DownloadArguments downloadArgs, bool forceRecache = false,
                                                                                ConcurrentDictionary<uint, Server> depotServers = null,
                                                                                ConcurrentDictionary<uint, ConcurrentDictionary<string, byte>> depotServerBlacklist = null)
        {
            // depotServers is pre-seeded by the probe phase, but defaulted here so the benchmark commands can call this directly.
            depotServers ??= new ConcurrentDictionary<uint, Server>();
            depotServerBlacklist ??= new ConcurrentDictionary<uint, ConcurrentDictionary<string, byte>>();

            double requestTotalSize = requestsToDownload.Sum(e => e.CompressedLength);
            var progressTask = ctx.AddTask(taskTitle, new ProgressTaskSettings { MaxValue = requestTotalSize });

            var failedRequests = new ConcurrentBag<QueuedRequest>();

            // A shared snapshot of all available servers.  Rather than using a single server for the entire batch, each
            // depot is assigned a server independently, so that a 403 from one CDN (meaning that CDN doesn't cache that
            // particular depot) can fail over to a different server for that depot only.
            var availableServers = _cdnPool.GetServersByLoad();
            var assignmentLock = new object();

            bool IsBlacklisted(uint depotId, string host)
                => depotServerBlacklist.TryGetValue(depotId, out var hosts) && hosts.ContainsKey(host);

            // Returns the server currently assigned to a depot, picking a new one if the depot has no assignment yet or
            // its current server has been blacklisted.  Returns null when every available server has 403'd for the depot.
            Server GetServerForDepot(uint depotId)
            {
                if (depotServers.TryGetValue(depotId, out var assigned) && !IsBlacklisted(depotId, assigned.Host))
                {
                    return assigned;
                }

                lock (assignmentLock)
                {
                    // Re-check inside the lock, another thread may have already rotated to a valid server.
                    if (depotServers.TryGetValue(depotId, out var current) && !IsBlacklisted(depotId, current.Host))
                    {
                        return current;
                    }

                    var next = availableServers.FirstOrDefault(server => !IsBlacklisted(depotId, server.Host));
                    if (next != null)
                    {
                        depotServers[depotId] = next;
                    }
                    return next;
                }
            }

            await Parallel.ForEachAsync(requestsToDownload, new ParallelOptions { MaxDegreeOfParallelism = downloadArgs.MaxConcurrentRequests }, body: async (request, _) =>
            {
                try
                {
                    // Loop to allow failing over to a different server when a CDN returns a 403 for this depot.
                    while (true)
                    {
                        var cdnServer = GetServerForDepot(request.DepotId);
                        if (cdnServer == null)
                        {
                            throw new CdnExhaustionException($"All available CDN servers returned a 403 for depot {request.DepotId}.  This depot may not be cached by any available CDN.");
                        }

                        try
                        {
                            var url = $"http://{_lancacheAddress}/depot/{request.DepotId}/chunk/{request.ChunkId}";
                            if (forceRecache)
                            {
                                url += "?nocache=1";
                            }
                            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                            requestMessage.Headers.Host = cdnServer.Host;

                            using var cts = new CancellationTokenSource();
                            using var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                            using Stream responseStream = await response.Content.ReadAsStreamAsync(cts.Token);
                            response.EnsureSuccessStatusCode();

                            // Don't save the data anywhere, so we don't have to waste time writing it to disk.
                            var buffer = new byte[4096];
                            while (await responseStream.ReadAsync(buffer, cts.Token) != 0)
                            {
                            }
                            break;
                        }
                        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Forbidden)
                        {
                            // A 403 means this CDN doesn't cache this depot.  Blacklist it for this depot only, then loop
                            // to fail over to a different server.  The server stays available for other depots.
                            // TryAdd is used so we only log the failover once per (depot, server), instead of once per chunk.
                            var blacklistedHosts = depotServerBlacklist.GetOrAdd(request.DepotId, _ => new ConcurrentDictionary<string, byte>());
                            if (blacklistedHosts.TryAdd(cdnServer.Host, 0))
                            {
                                _ansiConsole.LogMarkupVerbose($"CDN {Cyan(cdnServer.Host)} returned a {LightYellow("403")} for depot {LightYellow(request.DepotId)}, failing over to another server");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    request.LastFailureReason = e;
                    failedRequests.Add(request);
                }
                progressTask.Increment(request.CompressedLength);
            });

            // Making sure the progress bar is always set to its max value, in-case some unexpected error leaves the progress bar showing as unfinished
            progressTask.Increment(progressTask.MaxValue);
            return failedRequests;
        }

        /// <summary>
        /// Probes each depot against the available CDN servers to find one that actually serves it.  Some CDNs don't
        /// cache every depot and will return a 403, so determining (and reporting) a working server up front avoids
        /// discovering that through a storm of failed requests during the bulk download.
        /// </summary>
        /// <returns>A map of depotId to the server that will be used to download it.  A depot will be absent if no server served it.</returns>
        private async Task<ConcurrentDictionary<uint, Server>> ProbeDepotServersAsync(List<QueuedRequest> requestsToDownload,
                                                                                        ConcurrentDictionary<uint, ConcurrentDictionary<string, byte>> depotServerBlacklist)
        {
            var availableServers = _cdnPool.GetServersByLoad();
            var depotServers = new ConcurrentDictionary<uint, Server>();

            // A single representative chunk per depot is enough to tell whether a server serves that depot.
            var probeRequests = requestsToDownload.GroupBy(e => e.DepotId).Select(e => e.First()).ToList();

            await Parallel.ForEachAsync(probeRequests, async (probe, _) =>
            {
                foreach (var server in availableServers)
                {
                    try
                    {
                        // nocache=1 forces the Lancache to consult the upstream CDN instead of serving a previously cached
                        // chunk.  Without it, an already-cached probe chunk would return 200 and mask a CDN that actually
                        // 403s this depot upstream, causing those 403s to surface later during the bulk download instead.
                        var url = $"http://{_lancacheAddress}/depot/{probe.DepotId}/chunk/{probe.ChunkId}?nocache=1";
                        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                        requestMessage.Headers.Host = server.Host;
                        // Only the response status matters for the probe, so request a single byte rather than transferring
                        // the full ~1MB chunk over a potentially slow connection.  A serving CDN returns 200/206, one that
                        // doesn't cache this depot still returns 403.
                        requestMessage.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);

                        using var cts = new CancellationTokenSource();
                        using var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                        if (response.StatusCode == HttpStatusCode.Forbidden)
                        {
                            // This CDN doesn't cache this depot.  Blacklist it so the download phase skips it too.
                            depotServerBlacklist.GetOrAdd(probe.DepotId, _ => new ConcurrentDictionary<string, byte>()).TryAdd(server.Host, 0);
                            continue;
                        }
                        response.EnsureSuccessStatusCode();

                        depotServers[probe.DepotId] = server;
                        _ansiConsole.LogMarkupLine($"Depot {Cyan(probe.DepotId)} will download from CDN {LightYellow(server.Host)}");
                        return;
                    }
                    catch (Exception e)
                    {
                        // A transient (non-403) error doesn't mean the server can't serve this depot, so don't blacklist it.
                        // Just move on to the next candidate - the download phase can still fail over to it if needed.
                        FileLogger.LogExceptionNoStackTrace($"Probe for depot {probe.DepotId} on {server.Host} failed", e);
                    }
                }

                // No server served this depot during probing.  The download phase will still attempt its own failover,
                // so leave the depot unseeded rather than failing outright here.
                _ansiConsole.LogMarkupVerbose($"No CDN server served depot {LightYellow(probe.DepotId)} during probing");
            });

            return depotServers;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}