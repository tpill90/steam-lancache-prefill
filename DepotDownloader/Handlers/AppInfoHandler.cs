using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2;
using PicsProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader.Handlers
{
    //TODO document
    public class AppInfoHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly Steam3Session _steam3Session;

        // TODO make private
        public Dictionary<uint, AppInfoShim> LoadedAppInfos { get; private set; } = new Dictionary<uint, AppInfoShim>();
        
        public AppInfoHandler(IAnsiConsole ansiConsole, Steam3Session steam3Session)
        {
            _ansiConsole = ansiConsole;
            _steam3Session = steam3Session;
        }

        /// <summary>
        /// Will return an AppInfo for the specified AppId, that contains various metadata about the app.
        /// If the information for the specified app hasn't already been retrieved, then a request to the Steam network will be made.
        /// </summary>
        public async Task<AppInfoShim> GetAppInfo(uint appId)
        {
            if (LoadedAppInfos.ContainsKey(appId))
            {
                return LoadedAppInfos[appId];
            }

            await BulkLoadAppInfos(new List<uint> { appId });
            return LoadedAppInfos[appId];
        }

        /// <summary>
        /// Retrieves the latest AppInfo for multiple apps at the same time.  One large request containing multiple apps is significantly faster
        /// than multiple individual requests, as it seems that there is a minimum threshold for how quickly steam will return results.
        /// </summary>
        /// <param name="appIds">The list of App Ids to retrieve info for</param>
        public async Task BulkLoadAppInfos(List<uint> appIds)
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
                //TODO filter out tools and stuff here?
                LoadedAppInfos.Add(app.ID, new AppInfoShim(app.ID, app.ChangeNumber, app.KeyValues));
            }
        }

        //TODO document
        public List<uint> GetOwnedDlcAppIds()
        {
            return LoadedAppInfos.Values
                                 .SelectMany(e => e.DlcAppIds)
                                 .Where(e => _steam3Session.AccountHasAppAccess(e))
                                 .Distinct()
                                 .ToList();
        }

        //TODO document
        public async Task BuildDlcDepotList()
        {
            foreach (var app in LoadedAppInfos.Values)
            {
                //TODO I don't like how many times I have to filter down by owned app ids
                foreach (var dlcId in app.DlcAppIds.Where(e => _steam3Session.AccountHasAppAccess(e)))
                {
                    var dlcApp = await GetAppInfo(dlcId);
                    if (dlcApp.Depots != null)
                    {
                        app.Depots.AddRange(dlcApp.Depots);
                    }
                }
            }
        }

        // TODO document
        //TODO need to filter out apps that don't support the specified operating system
        public async Task<List<AppInfoShim>> FilterUnavailableApps(List<uint> appIds)
        {
            var result = new List<AppInfoShim>();
            foreach (var appId in appIds)
            {
                var appInfo = await GetAppInfo(appId);
                if (appInfo.State == "eStateUnAvailable")
                {
                    continue;
                }
                if (appInfo.Common != null && appInfo.Common.Type.ToLower() != "game")
                {
                    continue;
                }
                // Checking for invalid apps
                if (appInfo.Depots == null && appInfo.Common == null)
                {
                    //TODO log this to a file, or see if it keeps happening after finding an alternative way to list user apps
                    //_ansiConsole.LogMarkupLine(Red("Unknown/Invalid AppID ") + appId + Red(".  Skipping..."));
                    continue;
                }
                //TODO this doesn't seem to be working correctly for games I don't own
                if (!_steam3Session.AccountHasAppAccess(appInfo.AppId))
                {
                    _ansiConsole.LogMarkupLine(Red($"App {appInfo.AppId} ({appInfo.Common.Name}) is not available from this account."));
                    continue;
                }
                result.Add(appInfo);
            }
            // Whitespace divider
            _ansiConsole.WriteLine();

            return result.OrderBy(e => e.Common.Name).ToList();
        }
    }
}
