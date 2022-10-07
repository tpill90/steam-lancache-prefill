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

            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            _client.DefaultRequestHeaders.Add("User-Agent", "Valve/Steam HTTP Client 1.0");
        }

        /// <summary>
        /// Attempts to download all queued requests.  If all downloads are successful, will return true.
        /// In the case of any failed downloads, the failed downloads will be retried up to 3 times.  If the downloads fail 3 times, then
        /// false will be returned
        /// </summary>
        /// <returns>True if all downloads succeeded.  False if downloads failed 3 times.</returns>
        public async Task<bool> DownloadQueuedChunksAsync(List<QueuedRequest> queuedRequests, DownloadArguments downloadArgs, uint cellId)
        {
#if DEBUG
            if (AppConfig.SkipDownloads)
            {
                return true;
            }
#endif
            if (_lancacheAddress == null)
            {
                _lancacheAddress = await LancacheIpResolver.ResolveLancacheIpAsync(_ansiConsole, AppConfig.SteamCdnUrl);
            }
            
            int maxRetries = 3;

            var failedRequests = new ConcurrentBag<QueuedRequest>();
            await _ansiConsole.CreateSpectreProgress(downloadArgs.TransferSpeedUnit).StartAsync(async ctx =>
            {
                double requestTotalSize = queuedRequests.Sum(e => e.CompressedLength);
                var downloadTask = ctx.AddTask("Downloading...", new ProgressTaskSettings { MaxValue = requestTotalSize });
                // Run the initial download
                failedRequests = await AttemptDownloadAsync(ctx, queuedRequests, downloadTask);


                // Handle any failed requests
                if (failedRequests.Any())
                {
                    var task = ctx.AddTask("Retrying failed Requests", new ProgressTaskSettings() { MaxValue = maxRetries });
                    for (int retryCount = 0; retryCount < maxRetries && failedRequests.Any(); retryCount++)
                    {
                        task.Increment(1);
                        await _cdnPool.PopulateAvailableServersAsync(cellId);
                        await Task.Delay(500 * retryCount);
                        failedRequests = await AttemptDownloadAsync(ctx, failedRequests.ToList(), downloadTask);
                    }
                }

                if (failedRequests.Any())
                {
                    downloadTask.Description = "Failed.";
                    downloadTask.StopTask();
                }
            });

            // Handling final failed requests
            if (!failedRequests.Any())
            {
                return true;
            }

            _ansiConsole.MarkupLine(Red($"{failedRequests.Count} failed downloads"));
            return false;
        }
        
        /// <summary>
        /// Attempts to download the specified requests.  Returns a list of any requests that have failed.
        /// </summary>
        /// <returns>A list of failed requests</returns>
        [SuppressMessage("Reliability", "CA2016:Forward the 'CancellationToken' parameter to methods", Justification = "Don't have a need to cancel")]
        private async Task<ConcurrentBag<QueuedRequest>> AttemptDownloadAsync(ProgressContext ctx, List<QueuedRequest> requestsToDownload, ProgressTask task)
        {
            var failedRequests = new ConcurrentBag<QueuedRequest>();

            // Running as many requests as possible in parallel, evenly distributed across 3 cdns
            var cdnServers = _cdnPool.TakeConnections(3).ToList();
            var connCount = cdnServers.Count;
            await Parallel.ForEachAsync(requestsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (request, _) =>
            {
                var buffer = new byte[4096];
                try
                {
                    var url = ZString.Format("http://{0}/depot/{1}/chunk/{2}", _lancacheAddress, request.DepotId, request.ChunkId);
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    // Evenly distributes requests the available CDNs
                    requestMessage.Headers.Host = cdnServers[request.ChunkNum % connCount].Host;

                    var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                    using Stream responseStream = await response.Content.ReadAsStreamAsync();
                    response.EnsureSuccessStatusCode();

                    // Don't save the data anywhere, so we don't have to waste time writing it to disk.
                    while (await responseStream.ReadAsync(buffer, 0, buffer.Length, _) != 0)
                    {
                    }
                    task.Increment(request.CompressedLength); //Increment only on successfull downloads
                }
                catch
                {
                    failedRequests.Add(request);
                }
            });

            // Only return the connections for reuse if there were no errors
            if (failedRequests.IsEmpty)
            {
                _cdnPool.ReturnConnections(cdnServers);
            }
            return failedRequests;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}