namespace SteamPrefill.Handlers
{
    public sealed class DepotHandler
    {
        private readonly Steam3Session _steam3Session;
        private readonly AppInfoHandler _appInfoHandler;
        private readonly ManifestHandler _manifestHandler;

        /// <summary>
        /// KeyValue store of DepotId/ManifestId, that keeps a history of which manifest version(s) have been downloaded for each depot id.
        /// If a manifest version is found for a specific depot, than that depot can be considered as previously downloaded.
        /// </summary>
        private readonly Dictionary<uint, HashSet<ulong>> _downloadedDepots = new Dictionary<uint, HashSet<ulong>>();

        public DepotHandler(IAnsiConsole ansiConsole, Steam3Session steam3Session, AppInfoHandler appInfoHandler, CdnPool cdnPool, DownloadArguments downloadArgs)
        {
            _steam3Session = steam3Session;
            _appInfoHandler = appInfoHandler;
            _manifestHandler = new ManifestHandler(ansiConsole, cdnPool, steam3Session, downloadArgs);

            if (File.Exists(AppConfig.SuccessfullyDownloadedDepotsPath))
            {
                var fileContents = File.ReadAllText(AppConfig.SuccessfullyDownloadedDepotsPath);
                _downloadedDepots = JsonSerializer.Deserialize(fileContents, SerializationContext.Default.DictionaryUInt32HashSetUInt64);
            }
        }

        public void MarkDownloadAsSuccessful(List<DepotInfo> depots)
        {
            foreach (var depot in depots)
            {
                var depotId = depot.DepotId;

                // Initialize the entry for the specified depot
                if (!_downloadedDepots.ContainsKey(depotId))
                {
                    _downloadedDepots.Add(depotId, new HashSet<ulong>());
                }

                var downloadedManifests = _downloadedDepots[depotId];
                downloadedManifests.Add(depot.ManifestId.Value);
            }
            File.WriteAllText(AppConfig.SuccessfullyDownloadedDepotsPath, JsonSerializer.Serialize(_downloadedDepots, SerializationContext.Default.DictionaryUInt32HashSetUInt64));
        }

        /// <summary>
        /// An depot will be considered up to date if it's current version (manifest) has been previously downloaded.
        /// Thus, an app will be considered up to date if all of it's depots latest manifests have been previously downloaded.
        /// </summary>
        public bool AppIsUpToDate(List<DepotInfo> depots)
        {
            return depots.All(e => _downloadedDepots.ContainsKey(e.DepotId)
                                   && _downloadedDepots[e.DepotId].Contains(e.ManifestId.Value));
        }

        /// <summary>
        /// Filters depots based on the language/operating system/cpu architecture specified in the DownloadArguments
        /// </summary>
        public async Task<List<DepotInfo>> FilterDepotsToDownloadAsync(DownloadArguments downloadArgs, List<DepotInfo> allDepots)
        {
            var filteredDepots = new List<DepotInfo>();

            foreach (var depot in allDepots)
            {
                if (ExcludedDepots.Ids.Contains(depot.DepotId))
                {
                    continue;
                }

                // User must have access to a depot in order to download it
                if (!_steam3Session.LicenseManager.AccountHasDepotAccess(depot.DepotId))
                {
                    continue;
                }
                // Sometimes a linked ID can be an unowned app
                if (!_steam3Session.LicenseManager.AccountHasAppAccess(depot.ContainingAppId))
                {
                    continue;
                }

                AppInfo containingApp = await _appInfoHandler.GetAppInfoAsync(depot.ContainingAppId);
                if (containingApp.IsInvalidApp)
                {
                    continue;
                }

                // Checks to see if the depot supports any of the desired operating systems
                var osIsSupported = downloadArgs.OperatingSystems.Any(e => depot.SupportedOperatingSystems.Contains(e));
                if (depot.SupportedOperatingSystems.Any() && !osIsSupported)
                {
                    continue;
                }

                // Architecture
                if (depot.Architecture != null && depot.Architecture != downloadArgs.Architecture)
                {
                    continue;
                }

                // Language
                if (depot.Languages.Any() && !depot.Languages.Contains(downloadArgs.Language))
                {
                    continue;
                }

                // Low Violence 
                if (depot.LowViolence != null && depot.LowViolence.Value)
                {
                    continue;
                }
                filteredDepots.Add(depot);
            }
            return filteredDepots;
        }

        //TODO comment
        //TODO I don't like the fact that this has to be manually called in order to have things work correctly
        public async Task BuildLinkedDepotInfoAsync(List<DepotInfo> depots)
        {
            foreach (var depotInfo in depots.Where(e => e.ManifestId == null))
            {
                // Shared depots will have to go get the manifest id from the original app's depot
                var linkedApp = await _appInfoHandler.GetAppInfoAsync(depotInfo.DepotFromApp.Value);
                var linkedDepot = linkedApp.Depots.First(e => e.DepotId == depotInfo.DepotId);
                depotInfo.ManifestId = linkedDepot.ManifestId;
            }
        }

        /// <summary>
        /// Downloads all of the required manifests for a game, and then combines all of the required chunk requests into a single queue.
        /// </summary>
        /// <returns></returns>
        public async Task<List<QueuedRequest>> BuildChunkDownloadQueueAsync(List<DepotInfo> depots)
        {
            var depotManifests = await _manifestHandler.GetAllManifestsAsync(depots);

            // Queueing up chunks for each depot
            var chunkQueue = new List<QueuedRequest>();
            foreach (var depotManifest in depotManifests)
            {
                // A depot will contain multiple files, that are broken up into 1MB chunks
                var dedupedChunks = depotManifest.Files
                                                 .SelectMany(e => e.Chunks)
                                                 // Steam appears to do block level deduplication, so it is possible for multiple files to have the same chunk id
                                                 .DistinctBy(e => e.ChunkId)
                                                 .ToList();

                foreach (ChunkData chunk in dedupedChunks)
                {
                    chunkQueue.Add(new QueuedRequest(depotManifest, chunk));
                }
            }
            return chunkQueue;
        }
    }
}