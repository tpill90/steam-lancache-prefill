using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Cysharp.Text;
using SteamPrefill.Models;
using SteamPrefill.Settings;
using SteamPrefill.Steam;
using SteamPrefill.Utils;
using Spectre.Console;
using static SteamPrefill.Utils.SpectreColors;

namespace SteamPrefill.Handlers
{
    //TODO document
    public class DownloadHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly CdnPool _cdnPool;
        private readonly HttpClient _client = new HttpClient();
        
        public DownloadHandler(IAnsiConsole ansiConsole, CdnPool cdnPool)
        {
            _ansiConsole = ansiConsole;
            _cdnPool = cdnPool;
        }

        /// <summary>
        /// Attempts to download all queued requests.  If all downloads are successful, will return true.
        /// In the case of any failed downloads, the failed downloads will be retried up to 3 times.  If the downloads fail 3 times, then
        /// false will be returned
        /// </summary>
        /// <returns>True if all downloads succeeded.  False if downloads failed 3 times.</returns>
        public async Task<bool> DownloadQueuedChunksAsync(List<QueuedRequest> queuedRequests)
        {
            if (AppConfig.SkipDownload)
            {
                return true;
            }

            int retryCount = 0;

            var failedRequests = new ConcurrentBag<QueuedRequest>();
            await _ansiConsole.CreateSpectreProgress().StartAsync(async ctx =>
            {
                // Run the initial download
                failedRequests = await AttemptDownloadAsync(ctx, "Downloading..", queuedRequests);

                // Handle any failed requests
                while (failedRequests.Any() && retryCount < 3)
                {
                    retryCount++;
                    await Task.Delay(2000 * retryCount);
                    failedRequests = await AttemptDownloadAsync(ctx, $"Retrying  {retryCount}..", failedRequests.ToList());
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

        //TODO this doesn't always consistently hit 10gbit
        /// <summary>
        /// Attempts to download the specified requests.  Returns a list of any requests that have failed.
        /// </summary>
        /// <returns>A list of failed requests</returns>
        [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "Want to catch all exceptions, regardless of type")]
        private async Task<ConcurrentBag<QueuedRequest>> AttemptDownloadAsync(ProgressContext ctx, string taskTitle, List<QueuedRequest> requestsToDownload)
        {
            double requestTotalSize = requestsToDownload.Sum(e => e.CompressedLength);
            var progressTask = ctx.AddTask(taskTitle, new ProgressTaskSettings { MaxValue = requestTotalSize });

            var failedRequests = new ConcurrentBag<QueuedRequest>();

            // Breaking up requests into smaller batches, to distribute the load across multiple CDNs.  Steam appears to get better download speeds when doing this.
            int totalErrors = 0;
            var cdnServer = _cdnPool.TakeConnection();

            // Running multiple requests in parallel on a single CDN
            await Parallel.ForEachAsync(requestsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (request, _) =>
            {
                var buffer = new byte[4096];
                try
                {
                    var url = ZString.Format("http://{0}/depot/{1}/chunk/{2}", cdnServer.Host, request.DepotId, request.ChunkId);
                    var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    using Stream responseStream = await response.Content.ReadAsStreamAsync();
                    response.EnsureSuccessStatusCode();

                    // Don't save the data anywhere, so we don't have to waste time writing it to disk.
                    while (await responseStream.ReadAsync(buffer, 0, buffer.Length, _) != 0)
                    {
                    }
                }
                catch
                {
                    totalErrors++;
                    failedRequests.Add(request);
                }
                progressTask.Increment(request.CompressedLength);
            });

            // Only return the connection for reuse if there were no errors
            if (totalErrors == 0)
            {
                _cdnPool.ReturnConnection(cdnServer);
            }

            // Making sure the progress bar is always set to its max value, incase some unexpected error leaves the progress bar showing as unfinished
            progressTask.Increment(progressTask.MaxValue);
            return failedRequests;
        }
    }
}