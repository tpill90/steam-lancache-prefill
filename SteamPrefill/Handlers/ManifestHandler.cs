namespace SteamPrefill.Handlers
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
        private readonly IAnsiConsole _ansiConsole;
        private readonly CdnPool _cdnPool;
        private readonly Steam3Session _steam3Session;
        private readonly DownloadArguments _downloadArguments;

        private const int MaxRetries = 3;

        public ManifestHandler(IAnsiConsole ansiConsole, CdnPool cdnPool, Steam3Session steam3Session, DownloadArguments downloadArguments)
        {
            _ansiConsole = ansiConsole;
            _cdnPool = cdnPool;
            _steam3Session = steam3Session;
            _downloadArguments = downloadArguments;
        }

        /// <summary>
        /// Downloads all of the manifests for a list of specified depots.  Will retry up to 3 times, in the case of errors.
        /// </summary>
        /// <exception cref="ManifestException"></exception>
        public async Task<ConcurrentBag<Manifest>> GetAllManifestsAsync(List<DepotInfo> depots)
        {
            var manifestTimer = Stopwatch.StartNew();
            int attempts = 0;
            var depotManifests = new ConcurrentBag<Manifest>();
            while (depotManifests.Count != depots.Count && attempts < MaxRetries)
            {
                try
                {
                    depotManifests = await AttemptManifestDownloadAsync(depots);
                }
                catch (Exception e)
                {
                    // Regardless of the manifest, we're always going to retry multiple times
                    _ansiConsole.MarkupLine(Red("An error occurred while downloading manifests.  Retrying..."));

                    // Log extended details to disk
                    FileLogger.Log("An exception occurred while downloading manifests");
                    FileLogger.Log(e.ToString());

                    // Pausing a short time, in case the error was transient
                    await Task.Delay(500 * attempts);
                }
                attempts++;
            }
            if (attempts == MaxRetries)
            {
                throw new ManifestException("An unexpected error occurred while downloading manifests!  Skipping...");
            }

#if DEBUG
            _ansiConsole.LogMarkupLine($"Downloaded {Magenta(depotManifests.Count)} manifests in {LightYellow(manifestTimer.FormatElapsedString())}");
#endif

            return depotManifests;
        }
        
        private async Task<ConcurrentBag<Manifest>> AttemptManifestDownloadAsync(List<DepotInfo> depots)
        {
            var depotManifests = new ConcurrentBag<Manifest>();

            await _ansiConsole.StatusSpinner().StartAsync("Fetching depot manifests...", async _ =>
            {
                Server server = _cdnPool.TakeConnection();
                await Parallel.ForEachAsync(depots, new ParallelOptions { MaxDegreeOfParallelism = 5 }, async (depot, _) =>
                {
                    var manifest = await GetSingleManifestAsync(depot, server);
                    depotManifests.Add(manifest);
                });
                _cdnPool.ReturnConnection(server);
            });
            return depotManifests;
        }

        /// <summary>
        /// Requests a depot's manifest from Steam's servers, or if it has been requested before,
        /// it will load the manifest from disk.
        /// </summary>
        /// <param name="depot">The depot to download a manifest for</param>
        /// <returns>A manifest file</returns>
        /// <exception cref="ManifestException">Throws if no manifest was returned by Steam</exception>
        private async Task<Manifest> GetSingleManifestAsync(DepotInfo depot, Server server)
        {
            if (!_downloadArguments.NoCache && File.Exists(depot.ManifestFileName))
            {
                return Manifest.LoadFromFile(depot.ManifestFileName);
            }

            ManifestRequestCode manifestRequestCode = await GetManifestRequestCodeAsync(depot);
            DepotManifest manifest = await _steam3Session.CdnClient.DownloadManifestAsync(depot.DepotId, depot.ManifestId.Value, manifestRequestCode.Code, server);
            if (manifest == null)
            {
                throw new ManifestException($"Unable to download manifest for depot {depot.Name} - {depot.DepotId}.  Manifest request received no response.");
            }

            var protoManifest = new Manifest(manifest, depot);
            if (_downloadArguments.NoCache)
            {
                return protoManifest;
            }

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
        private async Task<ManifestRequestCode> GetManifestRequestCodeAsync(DepotInfo depot)
        {
            ulong manifestRequestCode = await _steam3Session.SteamContent.GetManifestRequestCode(depot.DepotId, depot.ContainingAppId, depot.ManifestId.Value, "public");
            
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