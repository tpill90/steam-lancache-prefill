namespace SteamPrefill.Handlers
{
    /// <summary>
    /// Responsible for downloading manifests from Steam, as well as loading previously saved manifests from disk.
    ///
    /// A manifest lists the files for a depot, as well as where they can be downloaded on Steam's CDN.
    /// A manifest typically represents a single "version" of a depot, so subsequent updates to the depot will have
    /// a different manifest.
    /// </summary>
    public sealed class ManifestHandler
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
        /// Downloads all of the manifests for a list of specified depots.
        /// Will retry up to 3 times, in the case of transient errors.
        /// If the download for a manifest has failed 3 times, it will skip downloading the current app.
        /// </summary>
        /// <exception cref="ManifestException"></exception>
        public async Task<List<Manifest>> GetAllManifestsAsync(List<DepotInfo> depots)
        {
            _ansiConsole.LogMarkupVerbose($"Downloading manifests for {Magenta(depots.Count)} depots");

            var depotManifests = new List<Manifest>();

            // Loading manifests already on disk in parallel
            var cachedManifestTasks = depots.Where(e => ManifestIsCached(e))
                                                        .Select(e => GetSingleManifestAsync(e))
                                                        .ToList();
            var resultManifests = await Task.WhenAll(cachedManifestTasks);
            depotManifests.AddRange(resultManifests);

            // Downloading un-cached depots from the internet
            foreach (var depot in depots.Where(e => !ManifestIsCached(e)).ToList())
            {
                int attempts = 0;
                Manifest manifest = null;
                while (manifest == null && attempts < MaxRetries)
                {
                    try
                    {

                        manifest = await GetSingleManifestAsync(depot);
                        depotManifests.Add(manifest);
                    }
                    catch (Exception e)
                    {
                        if (e is TaskCanceledException || e is TimeoutException)
                        {
                            _ansiConsole.LogMarkupError($"Manifest request timed out for depot {Cyan(depot.Name)} - {LightYellow(depot.DepotId)}.  Retrying...");
                        }
                        else if (e is SteamKitWebRequestException && e.Message.Contains("508"))
                        {
                            _ansiConsole.LogMarkupError("   An infinite loop was detected while downloading manifests.\n" +
                                                            "   This likely means that there is an issue with your network configuration.\n" +
                                                            "   Please check your configuration, and retry again.\n");
                            throw new InfiniteLoopException("Infinite loop detected while downloading manifests");
                        }
                        else
                        {
                            // Default catch all message
                            _ansiConsole.LogMarkupError($"   An unexpected error ({e.GetType()}) occurred while downloading manifests.  Retrying...");
                        }
                        FileLogger.LogException("An exception occurred while downloading manifests", e);
                    }

                    // Pausing a short time, in case the error was transient
                    attempts++;
                    await Task.Delay(500 * attempts);

                    if (attempts == MaxRetries)
                    {
                        throw new ManifestException("Unable to download manifests!");
                    }
                }
            }
            return depotManifests;
        }

        /// <summary>
        /// Requests a depot's manifest from Steam's servers, or if it has been requested before,
        /// it will load the manifest from disk.
        /// </summary>
        /// <param name="depot">The depot to download a manifest for</param>
        /// <returns>A manifest file</returns>
        /// <exception cref="ManifestException">Throws if no manifest was returned by Steam</exception>
        private async Task<Manifest> GetSingleManifestAsync(DepotInfo depot)
        {
            if (ManifestIsCached(depot))
            {
                return Manifest.LoadFromFile(depot.ManifestFileName);
            }

            _ansiConsole.LogMarkupVerbose($"Downloading manifest {LightYellow(depot.ManifestId)} for depot {Cyan(depot.DepotId)}");

            ManifestRequestCode manifestRequestCode = await GetManifestRequestCodeAsync(depot);

            Server server = _cdnPool.TakeConnection();
            DepotManifest manifest = await _steam3Session.CdnClient.DownloadManifestAsync(depot.DepotId, depot.ManifestId.Value, manifestRequestCode.Code, server);
            if (manifest == null)
            {
                throw new ManifestException($"Unable to download manifest for depot {depot.Name} - {depot.DepotId}.  Manifest request received no response.");
            }
            _cdnPool.ReturnConnection(server);

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

        private bool ManifestIsCached(DepotInfo depot)
        {
            return !_downloadArguments.NoCache && File.Exists(depot.ManifestFileName);
        }
    }
}