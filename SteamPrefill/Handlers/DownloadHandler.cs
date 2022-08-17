using System;
using Cysharp.Text;
using Spectre.Console;
using SteamPrefill.Handlers.Steam;
using SteamPrefill.Models;
using SteamPrefill.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SteamPrefill.Settings;
using static SteamPrefill.Utils.SpectreColors;

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

        public DownloadHandler(IAnsiConsole ansiConsole, CdnPool cdnPool, DownloadArguments downloadArguments)
        {
            _ansiConsole = ansiConsole;
            _cdnPool = cdnPool;

            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        /// <summary>
        /// Attempts to download all queued requests.  If all downloads are successful, will return true.
        /// In the case of any failed downloads, the failed downloads will be retried up to 3 times.  If the downloads fail 3 times, then
        /// false will be returned
        /// </summary>
        /// <returns>True if all downloads succeeded.  False if downloads failed 3 times.</returns>
        public async Task<bool> DownloadQueuedChunksAsync(List<QueuedRequest> queuedRequests)
        {
            if (_lancacheAddress == null)
            {
                _lancacheAddress = await LancacheIpResolver.ResolveLancacheIpAsync(_ansiConsole, AppConfig.SteamCdnUrl);
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
        
        /// <summary>
        /// Attempts to download the specified requests.  Returns a list of any requests that have failed.
        /// </summary>
        /// <returns>A list of failed requests</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2016:Forward the 'CancellationToken' parameter to methods", Justification = "Don't have a need to cancel")]
        private async Task<ConcurrentBag<QueuedRequest>> AttemptDownloadAsync(ProgressContext ctx, string taskTitle, List<QueuedRequest> requestsToDownload)
        {
            double requestTotalSize = requestsToDownload.Sum(e => e.CompressedLength);
            var progressTask = ctx.AddTask(taskTitle, new ProgressTaskSettings { MaxValue = requestTotalSize });

            var failedRequests = new ConcurrentBag<QueuedRequest>();

            var cdnServer = _cdnPool.TakeConnection();

            // Running multiple requests in parallel on a single CDN
            await Parallel.ForEachAsync(requestsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (request, _) =>
            {
                //TODO implement shared buffer pool.  Seems to have a massive performance improvement in BattlenetPrefill when running on the server
                //byte[] buffer = ArrayPool<byte>.Shared.Rent(524_288);
                var buffer = new byte[4096];
                try
                {
                    var url = ZString.Format("http://{0}/depot/{1}/chunk/{2}", _lancacheAddress, request.DepotId, request.ChunkId);
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                    requestMessage.Headers.Host = cdnServer.Host;
                    
                    var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                    using Stream responseStream = await response.Content.ReadAsStreamAsync();
                    response.EnsureSuccessStatusCode();

                    // Don't save the data anywhere, so we don't have to waste time writing it to disk.
                    while (await responseStream.ReadAsync(buffer, 0, buffer.Length, _) != 0)
                    {
                    }
                }
                catch
                {
                    failedRequests.Add(request);
                }
                progressTask.Increment(request.CompressedLength);
            });

            // Only return the connection for reuse if there were no errors
            if (failedRequests.IsEmpty)
            {
                _cdnPool.ReturnConnection(cdnServer);
            }

            // Making sure the progress bar is always set to its max value, incase some unexpected error leaves the progress bar showing as unfinished
            progressTask.Increment(progressTask.MaxValue);
            return failedRequests;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}