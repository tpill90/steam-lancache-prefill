using System;
using Cysharp.Text;
using Spectre.Console;
using SteamPrefill.Handlers.Steam;
using SteamPrefill.Models;
using SteamPrefill.Models.Exceptions;
using SteamPrefill.Utils;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static SteamPrefill.Utils.SpectreColors;

namespace SteamPrefill.Handlers
{
    public sealed class DownloadHandler : IDisposable
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly CdnPool _cdnPool;
        private readonly HttpClient _client;
        
        public DownloadHandler(IAnsiConsole ansiConsole, CdnPool cdnPool)
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
            await ValidateLancacheIpAsync();

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

            // Breaking up requests into smaller batches, to distribute the load across multiple CDNs.  Steam appears to get better download speeds when doing this.
            var cdnServer = _cdnPool.TakeConnection();

            // Running multiple requests in parallel on a single CDN
            await Parallel.ForEachAsync(requestsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (request, _) =>
            {
                var buffer = new byte[4096];
                try
                {
                    var url = ZString.Format("http://lancache.steamcontent.com/depot/{0}/chunk/{1}", request.DepotId, request.ChunkId);
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

        private bool _lancacheServerResolved;
        private bool _publicDownloadOverride;

        private async Task ValidateLancacheIpAsync()
        {
            // Short circuit if we have already determined that we can connect to a correctly configured Lancache
            if (_lancacheServerResolved || _publicDownloadOverride)
            {
                return;
            }

            var ipAddresses = await Dns.GetHostAddressesAsync("lancache.steamcontent.com");
            if (ipAddresses.Any(e => e.IsInternal()))
            {
                // If the IP resolves to a private subnet, then we want to query the Lancache server to see if it is actually there.
                var response = await _client.GetAsync(new Uri("http://lancache.steamcontent.com/lancache-heartbeat"));
                if (!response.Headers.Contains("X-LanCache-Processed-By"))
                {
                    _ansiConsole.MarkupLine(Red($" Error!  {White("lancache.steamcontent.com")} is resolving to a private IP address {Cyan($"({ipAddresses.First()})")},\n" +
                                                 " however no Lancache can be found at that address.\n" +
                                                 " Please check your configuration, and try again.\n"));
                    throw new LancacheNotFoundException($"No Lancache server detected at {ipAddresses.First()}");
                }
                _lancacheServerResolved = true;
                return;
            }

            // If a public IP address is resolved, then it means that the Lancache is not configured properly, and we would end up downloading from the internet.
            // This will prompt a user to see if they still want to continue, as downloading from the internet could still be a good download speed test.
            _ansiConsole.MarkupLine(LightYellow($" Warning!  {White("lancache.steamcontent.com")} is resolving to a public IP address {Cyan($"({ipAddresses.First()})")}.\n" +
                                                " Prefill will download directly from the internet, and will not be cached by Lancache.\n"));

            _publicDownloadOverride = _ansiConsole.Prompt(new SelectionPrompt<bool>()
                                                          .Title("Continue anyway?")
                                                          .AddChoices(true, false)
                                                          .UseConverter(e => e == false ? "No" : "Yes"));
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}