using ByteSizeLib;
using SteamPrefill.Handlers;
using SteamPrefill.Models;
using SteamPrefill.Protos;
using SteamPrefill.Steam;
using SteamPrefill.Utils;
using Spectre.Console;
using Spectre.Console.Testing;
using static SteamPrefill.Utils.SpectreColors;

namespace Benchmark
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            await Setup();
            //await BenchmarkDownload();
        }

        private static List<QueuedRequest> _queuedRequests;
        private static CdnPool _cdnPool = new CdnPool(new TestConsole(), null);

        //TODO move this into a util class
        //public static async Task BenchmarkDownload()
        //{

        //    var totalBytes = ByteSize.FromBytes(_queuedRequests.Sum(e => e.chunk.CompressedLength));
        //    //TODO total download size is the wrong unit.
        //    AnsiConsole.Console.LogMarkupLine($"Downloading {Magenta(totalBytes.ToString())}");

        //    var downloadHandler = new DownloadHandler(AnsiConsole.Console, _cdnPool);
        //    await downloadHandler.DownloadQueuedChunksAsync(_queuedRequests);
        //}

        public static async Task Setup()
        {
            _queuedRequests = new List<QueuedRequest>();

            var manifestFolder = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\SteamPrefill\SteamPrefill\bin\Release\net6.0\ManifestCache\";
            var allFiles = Directory.GetFiles(manifestFolder).ToList();

            var skip = 0;
            foreach (var fileName in allFiles)
            {
                var manifest = ProtoManifest.LoadFromFile(fileName);
                var depotId = UInt32.Parse(fileName.Split("_")[1]);

                if (manifest == null)
                {
                    continue;
                }
                // A depot can be made up of multiple files
                //foreach (var file in manifest.Files)
                //{
                //    // A file larger than 1MB will need to be downloaded in multiple chunks
                //    foreach (var chunk in file.Chunks)
                //    {
                //        _queuedRequests.Add(new QueuedRequest
                //        {
                //            DepotId = depotId,
                //            chunk = chunk
                //        });
                //    }
                //}
            }

            await _cdnPool.PopulateAvailableServers();
        }
    }
}