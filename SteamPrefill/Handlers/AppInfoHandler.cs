using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SteamKit2;
using SteamPrefill.Handlers.Steam;
using SteamPrefill.Models.Enums;
using PicsProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;
using SteamPrefill.Models;

namespace SteamPrefill.Handlers
{
    /// <summary>
    /// Responsible for retrieving application metadata from Steam
    /// </summary>
    public class AppInfoHandler
    {
        private readonly Steam3Session _steam3Session;

        private Dictionary<uint, AppInfo> LoadedAppInfos { get; } = new Dictionary<uint, AppInfo>();

        public AppInfoHandler(Steam3Session steam3Session)
        {
            _steam3Session = steam3Session;
        }

        /// <summary>
        /// Will return an AppInfo for the specified AppId, that contains various metadata about the app.
        /// If the information for the specified app hasn't already been retrieved, then a request to the Steam network will be made.
        /// </summary>
        public async Task<AppInfo> GetAppInfoAsync(uint appId)
        {
            if (LoadedAppInfos.ContainsKey(appId))
            {
                return LoadedAppInfos[appId];
            }

            await BulkLoadAppInfosAsync(new List<uint> { appId });
            return LoadedAppInfos[appId];
        }

        /// <summary>
        /// Retrieves the latest AppInfo for multiple apps at the same time.  One large request containing multiple apps is significantly faster
        /// than multiple individual requests, as it seems that there is a minimum threshold for how quickly steam will return results.
        /// </summary>
        /// <param name="appIds">The list of App Ids to retrieve info for</param>
        public async Task BulkLoadAppInfosAsync(List<uint> appIds)
        {
            var appIdsToLoad = appIds.Where(e => !LoadedAppInfos.ContainsKey(e) && _steam3Session.AccountHasAppAccess(e)).ToList();
            if (!appIdsToLoad.Any())
            {
                return;
            }

            // Some apps will require an additional "access token" in order to retrieve their app metadata
            var accessTokensResponse = await _steam3Session.SteamAppsApi.PICSGetAccessTokens(appIds, new List<uint>()).ToTask();
            var appTokens = accessTokensResponse.AppTokens;

            // Build out the requests
            var requests = new List<SteamApps.PICSRequest>();
            foreach (var appId in appIdsToLoad)
            {
                var request = new SteamApps.PICSRequest(appId);
                if (appTokens.ContainsKey(appId))
                {
                    request.AccessToken = appTokens[appId];
                }
                requests.Add(request);
            }
            // Finally request the metadata from steam
            var resultSet = await _steam3Session.SteamAppsApi.PICSGetProductInfo(requests, new List<SteamApps.PICSRequest>()).ToTask();

            List<PicsProductInfo> appInfos = resultSet.Results.SelectMany(e => e.Apps).Select(e => e.Value).ToList();
            foreach (var app in appInfos)
            {
                LoadedAppInfos.Add(app.ID, new AppInfo(_steam3Session, app.ID, app.KeyValues));
            }
        }

        /// <summary>
        /// Steam stores all DLCs for a game as separate "apps", so they must be loaded after the game's AppInfo has been retrieved,
        /// and the list of DLC AppIds are known.
        ///
        /// Once the DLC apps are loaded, the final combined depot list (both the app + dlc apps) will be built.
        /// </summary>
        public async Task BulkLoadDlcAppInfoAsync()
        {
            var dlcAppIds = LoadedAppInfos.Values.SelectMany(e => e.DlcAppIds).ToList();
            await BulkLoadAppInfosAsync(dlcAppIds);

            // Builds out the list of all depots for each game, including depots from all related DLCs
            // DLCs are stored as separate "apps", so their info comes back separately.
            foreach (var app in LoadedAppInfos.Values.Where(e => e.Type == AppType.Game))
            {
                foreach (var dlcApp in app.DlcAppIds)
                {
                    app.Depots.AddRange((await GetAppInfoAsync(dlcApp)).Depots);
                }

                var distinctDepots = app.Depots.DistinctBy(e => e.DepotId).ToList();
                app.Depots.Clear();
                app.Depots.AddRange(distinctDepots);
            }
        }

        /// <summary>
        /// Gets a list of all available games.  Will be filtered down to only games that are available, and support Windows.
        /// </summary>
        /// <returns>All currently available games for the current user</returns>
        public List<AppInfo> GetAvailableGames()
        {
            var apps = LoadedAppInfos.Values.Where(e => e.Type == AppType.Game 
                                                        && e.State != ReleaseState.eStateUnAvailable 
                                                        && e.SupportsWindows)
                                            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                                            .ToList();

            return apps;
        }
    }
}