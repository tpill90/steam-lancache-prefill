using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DepotDownloader.Exceptions;
using DepotDownloader.Models;
using DepotDownloader.Protos;
using DepotDownloader.Steam;
using SteamKit2;
using SteamKit2.CDN;

namespace DepotDownloader.Handlers
{
    /// <summary>
    /// Responsible for downloading manifests from Steam, as well as loading previously saved manifests from disk.
    ///
    /// A manifest lists the files for a depot, as well as where they can be downloaded on Steam's CDN.
    /// A manifest typically represents a single "version" of a depot, so subsequent updates to the depot will have
    /// a different manifest.
    /// </summary>
    public class ManifestHandler
    {
        private readonly CdnPool _cdnPool;
        private readonly Steam3Session _steam3Session;

        public ManifestHandler(CdnPool cdnPool, Steam3Session steam3Session)
        {
            _cdnPool = cdnPool;
            _steam3Session = steam3Session;
        }

        //TODO test exception handling here, and in the calling method
        /// <summary>
        /// Requests a depot's manifest from Steam's servers, or if it has been requested before,
        /// it will load the manifest from disk.
        /// </summary>
        /// <param name="depot">The depot to download a manifest for</param>
        /// <returns>A manifest file</returns>
        /// <exception cref="ManifestException">Throws if no manifest was returned by Steam</exception>
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
                    Server server = _cdnPool.TakeConnection();
                    manifest = await _steam3Session.CdnClient.DownloadManifestAsync(depot.DepotId, depot.ManifestId.Value, manifestRequestCode.Code, server);
                    _cdnPool.ReturnConnection(server);
                }
                //TODO What other possible exceptions could happen here, that we can recover from?
                catch (HttpRequestException e)
                {
                    //TODO handle 503 service unavailable
                    if (e.StatusCode == HttpStatusCode.BadGateway || e.StatusCode == HttpStatusCode.ServiceUnavailable)
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
                throw new ManifestException($"Unable to download manifest for depot {depot.Name} - {depot.DepotId}.  Manifest request received no response.");
            }
            
            var protoManifest = new ProtoManifest(manifest, depot);
            protoManifest.SaveToFile(depot.ManifestFileName);

            return protoManifest;
        }

        /// <summary>
        /// Requests a ManifestRequestCode for the specified depot.  Each depot will have a unique code, that gets rotated every 5 minutes.
        /// These manifest codes are not unique to a user, so they will be used by all users in the same 5 minute window.
        ///
        /// These manifest codes act as a form of "authorization" for the CDN.  You can only download a manifest if your account has access to the
        /// specified depot, so since the CDN itself doesn't check for access, this will prevent unauthorized depot downloads
        ///
        /// https://steamdb.info/blog/manifest-request-codes/ 
        /// </summary>
        /// <param name="depot">The depot to request a manifest code for</param>
        /// <returns>A manifest code valid for 5 minutes.</returns>
        /// <exception cref="ManifestException">Throws if no valid manifest code was found</exception>
        private async Task<ManifestRequestCode> GetManifestRequestCode(DepotInfo depot)
        {
            ulong manifestRequestCode = await _steam3Session.steamContent.GetManifestRequestCode(depot.DepotId, depot.ContainingAppId, depot.ManifestId.Value, "public");
            
            // If we could not get the manifest code, this is a fatal error, as it we can't download the manifest without it.
            if (manifestRequestCode == 0)
            {
                throw new ManifestException($"No manifest request code was returned for {depot.DepotId} {depot.ManifestId.Value}");
            }

            return new ManifestRequestCode
            {
                Code = manifestRequestCode,
                RetrievedAt = DateTime.Now
            };
        }
    }
}
