namespace SteamPrefill.Handlers
{
    public class DepotHandler
    {
        private readonly Steam3Session _steam3Session;
        private readonly AppInfoHandler _appInfoHandler;

        /// <summary>
        /// KeyValue store of DepotId/ManifestId, that keeps a history of which manifest version(s) have been downloaded for each depot id.
        /// If a manifest version is found for a specific depot, than that depot can be considered as previously downloaded.
        /// </summary>
        private readonly Dictionary<uint, HashSet<ulong>> _downloadedDepots = new Dictionary<uint, HashSet<ulong>>();
        private readonly string _downloadedDepotsPath = $"{AppConfig.CacheDir}/successfullyDownloadedDepots.json";

        public DepotHandler(Steam3Session steam3Session, AppInfoHandler appInfoHandler)
        {
            _steam3Session = steam3Session;
            _appInfoHandler = appInfoHandler;
            
            if (File.Exists(_downloadedDepotsPath))
            {
                _downloadedDepots = JsonSerializer.Deserialize(File.ReadAllText(_downloadedDepotsPath), SerializationContext.Default.DictionaryUInt32HashSetUInt64);
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
                if (!downloadedManifests.Contains(depot.ManifestId.Value))
                {
                    downloadedManifests.Add(depot.ManifestId.Value);
                }
            }
            File.WriteAllText(_downloadedDepotsPath, JsonSerializer.Serialize(_downloadedDepots, SerializationContext.Default.DictionaryUInt32HashSetUInt64));
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
        
        public List<DepotInfo> FilterDepotsToDownload(DownloadArguments downloadArgs, List<DepotInfo> allDepots)
        {
            var filteredDepots = new List<DepotInfo>();

            foreach (var depot in allDepots)
            {
                // User must have access to a depot in order to download it
                if (!_steam3Session.AccountHasDepotAccess(depot.DepotId))
                {
                    continue;
                }
                //TODO test to make sure this doesn't have any performance implications
                AppInfo containingApp = _appInfoHandler.GetAppInfoAsync(depot.ContainingAppId).Result;
                if (containingApp.IsInvalidApp)
                {
                    continue;
                }

                // Filtering to only specified operating systems
                if (depot.SupportedOperatingSystems.Any() && !depot.SupportedOperatingSystems.Contains(downloadArgs.OperatingSystem))
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
    }
}