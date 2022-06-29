using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Protos;
using DepotDownloader.Steam;
using Spectre.Console;
using SteamKit2;

namespace DepotDownloader.Handlers
{
    //TODO document
    //TODO make not static
    public class ManifestHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly CdnPool _cdnPool;
        private readonly Steam3Session _steam3Session;

        public ManifestHandler(IAnsiConsole ansiConsole, CdnPool cdnPool, Steam3Session steam3Session)
        {
            _ansiConsole = ansiConsole;
            _cdnPool = cdnPool;
            _steam3Session = steam3Session;
        }

        //TODO document
        //TODO possibly write a retry loop?
        public async Task<ProtoManifest> GetManifestFile(DepotInfo depot)
        {
            if (File.Exists(depot.ManifestFileName))
            {
                return ProtoManifest.LoadFromFile(depot.ManifestFileName);
            }

            ManifestRequestCode manifestRequestCode = await GetManifestRequestCode(depot);

            DepotManifest manifest = null;
            var retryCount = 0;
            while (manifest == null && retryCount < 5)
            {
                try
                {
                    ServerShim server = _cdnPool.TakeConnection();
                    manifest = await _steam3Session.CdnClient.DownloadManifestAsync(depot.DepotId, depot.ManifestId.Value, manifestRequestCode.Code, server.ToSteamKitServer());
                    _cdnPool.ReturnConnection(server);
                }
                //TODO What other possible exceptions could happen here, that we can recover from?
                catch (HttpRequestException e)
                {
                    if (e.StatusCode == HttpStatusCode.BadGateway)
                    {
                        // In the case of a BadGateway, we'll want to retry again with a new server
                    }
                    else
                    {
                        throw;
                    }
                }
                retryCount++;
            }
            if (manifest == null && retryCount == 5)
            {
                //TODO better exception type
                //TODO how should this be handled by functions that call this?  
                throw new Exception($"Unable to download manifest for depot {depot.Name} - {depot.DepotId}");
            }
            
            var protoManifest = new ProtoManifest(manifest, depot);
            protoManifest.SaveToFile(depot.ManifestFileName);

            return protoManifest;
        }

        // TODO document
        // TODO include in docs -  ManifestRequestCodes can only be requested if you own the game. They act as a form of "authorization" for the CDN.
        // TODO  https://steamdb.info/blog/manifest-request-codes/ 
        private async Task<ManifestRequestCode> GetManifestRequestCode(DepotInfo depot)
        {
            var requestCode = await _steam3Session.steamContent.GetManifestRequestCode(depot.DepotId, depot.ContaingAppId.Value, depot.ManifestId.Value, "public");
            ulong manifestRequestCode = requestCode;

            // If we could not get the manifest code, this is a fatal error
            if (manifestRequestCode == 0)
            {
                //TODO handle error here
                _ansiConsole.WriteLine($"No manifest request code was returned for {depot.DepotId} {depot.ManifestId.Value}");
            }

            return new ManifestRequestCode
            {
                Code = manifestRequestCode,
                RetrievedAt = DateTime.Now
            };
        }
    }
}
