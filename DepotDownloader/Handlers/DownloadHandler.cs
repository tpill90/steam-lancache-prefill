using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader.Handlers
{
    //TODO document
    public class DownloadHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly CDNClientPool _cdnPool;
        private readonly HttpClient _client = new HttpClient();
        
        private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

        public DownloadHandler(IAnsiConsole ansiConsole, CDNClientPool cdnPool)
        {
            _ansiConsole = ansiConsole;
            _cdnPool = cdnPool;
        }

        //TODO comment
        public async Task DownloadQueuedChunksAsync(List<QueuedRequest> queuedRequests)
        {
            if (DownloadConfig.SkipDownload)
            {
                return;
            }
            int retryCount = 0;

            var failedRequests = new ConcurrentBag<QueuedRequest>();
            await _ansiConsole.CreateSpectreProgress().StartAsync(async ctx =>
            {
                // Run the initial download
                failedRequests = await AttemptDownloadAsync(ctx, "Downloading..", queuedRequests.ToList());

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
                return;
            }

            _ansiConsole.MarkupLine(Red($"{failedRequests.Count} failed chunks"));
            foreach (var failedRequest in failedRequests)
            {
                //TODO format this better
                _ansiConsole.WriteLine(failedRequest.PreviousError.ToString());
            }
        }

        /// <summary>
        /// Attempts to download the specified requests.  Returns a list of any requests that have failed.
        /// </summary>
        /// <returns>A list of failed requests</returns>
        [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "Want to catch all exceptions, regardless of type")]
        private async Task<ConcurrentBag<QueuedRequest>> AttemptDownloadAsync(ProgressContext ctx, string taskTitle, List<QueuedRequest> requestsToDownload)
        {
            double requestTotalSize = requestsToDownload.Sum(e => e.chunk.CompressedLength);
            var progressTask = ctx.AddTask(taskTitle, new ProgressTaskSettings { MaxValue = requestTotalSize });

            //TODO determine the server once
            var server = _cdnPool.GetConnection();

            var failedRequests = new ConcurrentBag<QueuedRequest>();
            await Parallel.ForEachAsync(requestsToDownload, new ParallelOptions { MaxDegreeOfParallelism = 8 }, async (request, _) =>
            {
                try
                {
                    await DownloadChunkAsync(request, progressTask, server);
                }
                catch
                {
                    failedRequests.Add(request);
                }
            });

            // Making sure the progress bar is always set to its max value, some files don't have a size, so the progress bar will appear as unfinished.
            progressTask.Increment(progressTask.MaxValue);
            return failedRequests;
        }

        //TODO comment
        private async Task DownloadChunkAsync(QueuedRequest request, ProgressTask progressTask, ServerShim connection)
        {
            var totalBytesRead = 0;
            var buffer = _bytePool.Rent(16384);

            try
            {
                var uri = new Uri($"http://{connection.Host}/depot/{request.depotDownloadInfo.DepotId}/chunk/{request.chunk}");
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

                using var responseMessage = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
                await using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync();
                responseMessage.EnsureSuccessStatusCode();

                while (true)
                {
                    // Dump the received data, so we don't have to waste time writing it to disk.
                    var read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0)
                    {
                        _bytePool.Return(buffer);
                        progressTask.Increment(totalBytesRead);
                        break;
                    }
                    totalBytesRead += read;
                }
            }
            catch(Exception e)
            {
                // Making sure that the current request is marked as "complete" in the progress bar, otherwise the progress bar will never hit 100%
                progressTask.Increment(request.chunk.UncompressedLength - totalBytesRead);
                _bytePool.Return(buffer);

                request.PreviousError = e;
                throw;
            }
        }
    }
}
