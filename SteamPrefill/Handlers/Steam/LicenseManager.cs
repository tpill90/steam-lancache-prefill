namespace SteamPrefill.Handlers.Steam
{
    public sealed class LicenseManager
    {
        private readonly SteamApps _steamAppsApi;

        /// <summary>
        /// Contains the purchase date for an app.  Key is the appId and the value is when it was purchased.
        /// </summary>
        private readonly Dictionary<uint, DateTime> _appPurchaseTimeLookup = new Dictionary<uint, DateTime>();

        internal UserLicenses _userLicenses = new UserLicenses();

        public List<uint> AllOwnedAppIds => _userLicenses.OwnedAppIds.ToList();

        public LicenseManager(SteamApps steamAppsApi)
        {
            _steamAppsApi = steamAppsApi;
        }

        /// <summary>
        /// Checks against the list of currently owned apps to determine if the user is able to download this app.
        /// </summary>
        /// <param name="appid">ID of the application to check for access</param>
        /// <returns>True if the user has access to the app</returns>
        public bool AccountHasAppAccess(uint appid)
        {
            return _userLicenses.OwnedAppIds.Contains(appid);
        }

        /// <summary>
        /// Checks against the list of currently owned depots + apps to determine if the user is able to download this depot.
        /// There are 3 cases that a depot is considered owned :
        /// - If a user owns an App, and the Package that grants the App ownership also grants ownership to the App's depots, then the user owns the depot.
        /// - If the user owns an App with DLC, and the App owns the depot + a Package grants ownership of the depot, then the user owns the depot.
        /// - If the user owns an App with DLC but DLC App has no depots of its own. And instead the App owns a depot with the same Id as the DLC App,
        ///     then the user owns the depot.  Example DLC AppId : 1962660 - DepotId : 1962660
        ///
        /// For official documentation on how this works, see : https://partner.steamgames.com/doc/store/application/dlc
        /// </summary>
        /// <param name="depotId">Id of the depot to check for access</param>
        /// <returns>True if the user has access to the depot</returns>
        public bool AccountHasDepotAccess(uint depotId)
        {
            return _userLicenses.OwnedDepotIds.Contains(depotId) || _userLicenses.OwnedAppIds.Contains(depotId);
        }

        [SuppressMessage("Threading", "VSTHRD002:Synchronously waiting on tasks or awaiters may cause deadlocks", Justification = "Callback must be synchronous to compile")]
        public void LoadPackageInfo(IReadOnlyCollection<LicenseListCallback.License> licenseList)
        {
            _userLicenses = new UserLicenses();

            // Filters out licenses that are subscription based, and have expired, like EA Play for example.
            // The account will continue to "own" the packages, and will be unable to download their apps, so they must be filtered out here.
            var nonExpiredLicenses = licenseList.Where(e => !e.LicenseFlags.HasFlag(ELicenseFlags.Expired)).ToList();

            // Some packages require an access token in order to request their apps/depot list
            var packageRequests = nonExpiredLicenses.Select(e => new PICSRequest(e.PackageID, e.AccessToken)).ToList();

            var jobResult = _steamAppsApi.PICSGetProductInfo(new List<PICSRequest>(), packageRequests).ToTask().Result;
            var packageInfos = jobResult.Results.SelectMany(e => e.Packages)
                                        .Select(e => e.Value)
                                        .Select(e => new Package(e.KeyValues))
                                        .OrderBy(e => e.Id)
                                        .ToList();

            // Processing the results
            var licenseDateLookup = nonExpiredLicenses.GroupBy(e => e.PackageID)
                                                      // It's possible to have more than one license for a game if you have a family share.  Picking the most recently purchased one.
                                                      .Select(e => e.OrderByDescending(e2 => e2.TimeCreated).Select(e2 => e2).First())
                                                      .ToDictionary(e => e.PackageID, e => e.TimeCreated);
            foreach (var package in packageInfos)
            {
                // Removing any free weekends that are no longer active
                if (package.IsFreeWeekend && package.FreeWeekendHasExpired)
                {
                    continue;
                }

                _userLicenses.OwnedAppIds.AddRange(package.AppIds);
                _userLicenses.OwnedDepotIds.AddRange(package.DepotIds);

                // Building out the AppID to purchase date lookup.
                foreach (var appId in package.AppIds)
                {
                    if (!_appPurchaseTimeLookup.ContainsKey(appId))
                    {
                        _appPurchaseTimeLookup.Add(appId, licenseDateLookup[package.Id]);
                    }
                }
            }

            _userLicenses.OwnedPackageIds.AddRange(packageInfos.Select(e => e.Id).ToList());
        }

        /// <summary>
        /// Gets a list of AppIDs for packages that were purchased/activated within the specified duration.
        /// </summary>
        /// <param name="recentDays">How recent the apps should have been purchased.  Ex, 14 will include any games purchased in the last two weeks.</param>
        /// <returns>A list of recently purchased AppIDs.</returns>
        public List<uint> GetRecentlyPurchasedAppIds(int recentDays)
        {
            var cutoffDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(recentDays));

            return _appPurchaseTimeLookup.ToList()
                                         .Where(e => e.Value >= cutoffDate)
                                         .Select(e => e.Key)
                                         .ToList();
        }

        public DateTime GetPurchaseDateForApp(uint appId)
        {
            return _appPurchaseTimeLookup[appId];
        }
    }

    public sealed class UserLicenses
    {
        public HashSet<uint> OwnedPackageIds { get; } = new HashSet<uint>();
        public HashSet<uint> OwnedAppIds { get; } = new HashSet<uint>();
        public HashSet<uint> OwnedDepotIds { get; } = new HashSet<uint>();

        public override string ToString()
        {
            return $"Packages : {OwnedPackageIds.Count} Apps : {OwnedAppIds.Count} Depots : {OwnedDepotIds.Count}";
        }
    }
}