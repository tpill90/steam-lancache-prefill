using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Protos;
using DepotDownloader.Steam;
using Spectre.Console;
using SteamKit2;
using SteamKit2.CDN;

namespace DepotDownloader.Handlers
{
    //TODO document
    //TODO make not static
    public static class ManifestHandler
    {
        public static async Task<DepotFilesData> GetManifestFilesAsync(DepotDownloadInfo depot, CDNClientPool cdnPool, Steam3Session steam3)
        {
            ProtoManifest manifest = await GetManifestFile(depot, cdnPool, steam3);
            return new DepotFilesData
            {
                depotDownloadInfo = depot,
                filteredFiles = manifest.Files.ToList()
            };
        }

        //TODO document
        public static async Task<ProtoManifest> GetManifestFile(DepotDownloadInfo depot, CDNClientPool cdnPool, Steam3Session steam3)
        {
            var manifestFileName = Path.Combine(DownloadConfig.ManifestCacheDir, $"{depot.appId}_{depot.id}_{depot.manifestId}.bin");
            if (File.Exists(manifestFileName))
            {
                return LoadManifestFromDisk(depot);
            }

            DepotManifest depotManifest = null;
            ulong manifestRequestCode = 0;
            var manifestRequestCodeExpiration = DateTime.MinValue;

            //TODO handle + test possible exceptions
            do
            {
                var now = DateTime.Now;

                // In order to download this manifest, we need the current manifest request code
                // The manifest request code is only valid for a specific period in time
                if (manifestRequestCode == 0 || now >= manifestRequestCodeExpiration)
                {
                    manifestRequestCode = await steam3.GetDepotManifestRequestCodeAsync(depot.id, depot.appId, depot.manifestId);
                    // This code will hopefully be valid for one period following the issuing period
                    manifestRequestCodeExpiration = now.Add(TimeSpan.FromMinutes(5));

                    // If we could not get the manifest code, this is a fatal error
                    if (manifestRequestCode == 0)
                    {
                        //TODO handle error here
                        Console.WriteLine("No manifest request code was returned for {0} {1}", depot.id, depot.manifestId);
                    }
                }
                // TODO only need to get the depot key if we haven't already downloaded the manifest
                steam3.RequestDepotKey(depot.id, depot.appId);
                if (!steam3.DepotKeys.ContainsKey(depot.id))
                {
                    //TODO better exception handling
                    AnsiConsole.WriteLine("No valid depot key for {0}, unable to download.", depot.id);
                    throw new Exception("No valid depot key");
                }

                //TODO store depot keys somewhere, and reload them.  They apparantly do not change when the manifest changes
                depot.depotKey = steam3.DepotKeys[depot.id];

                Server server = DownloadConfig.AutoMapper.Map<Server>(cdnPool.GetConnection());

                depotManifest = await cdnPool.CDNClient.DownloadManifestAsync(depot.id, depot.manifestId, manifestRequestCode, server, depot.depotKey);
             
            } while (depotManifest == null);
            
            var protoManifest = new ProtoManifest(depotManifest, depot.manifestId);
            protoManifest.SaveToFile(manifestFileName, out var checksum);

            File.WriteAllBytes(manifestFileName + ".sha", checksum);

            return protoManifest;
        }

        private static ProtoManifest LoadManifestFromDisk(DepotDownloadInfo depot)
        {
            var manifestFileName = Path.Combine(DownloadConfig.ManifestCacheDir, $"{depot.appId}_{depot.id}_{depot.manifestId}.bin");
            byte[] expectedChecksum = File.ReadAllBytes(manifestFileName + ".sha");

            byte[] currentChecksum;
            var manifest = ProtoManifest.LoadFromFile(manifestFileName, out currentChecksum);
            if (manifest != null && !expectedChecksum.SequenceEqual(currentChecksum))
            {
                //TODO better handling
                Console.WriteLine("Manifest {0} on disk did not match the expected checksum.", depot.manifestId);
                throw new Exception("Manifest checksum does not match expected");
            }
            return manifest;
        }
    }
}
