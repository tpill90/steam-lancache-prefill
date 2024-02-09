namespace SteamPrefill.Handlers
{
    /// <summary>
    /// Responsible for retrieving application metadata from Steam
    /// </summary>
    public class AppInfoHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly Steam3Session _steam3Session;
        private readonly LicenseManager _licenseManager;

        private List<CPlayer_GetOwnedGames_Response.Game> _ownedGames;

        /// <summary>
        /// A dictionary of all app metadata currently retrieved from Steam
        /// </summary>
        private ConcurrentDictionary<uint, AppInfo> LoadedAppInfos { get; } = new ConcurrentDictionary<uint, AppInfo>();

        public AppInfoHandler(IAnsiConsole ansiConsole, Steam3Session steam3Session, LicenseManager licenseManager)
        {
            _ansiConsole = ansiConsole;
            _steam3Session = steam3Session;
            _licenseManager = licenseManager;
        }

        #region Loading Metadata

        /// <summary>
        /// Gets the latest app metadata from steam, for the specified apps, as well as their related DLC apps
        /// </summary>
        public async Task RetrieveAppMetadataAsync(List<uint> appIds, bool loadDlcApps = true, bool loadRecentlyPlayed = false)
        {
            await _ansiConsole.StatusSpinner().StartAsync("Retrieving latest App metadata...", async _ =>
            {
                await BulkLoadAppInfoAsync(appIds);

                if (loadDlcApps)
                {
                    // Once we have loaded all the apps, we can also load information for related DLC
                    await FetchDlcAppInfoAsync();
                }

                if (loadRecentlyPlayed)
                {
                    // Populating play time
                    foreach (var app in await GetRecentlyPlayedGamesAsync())
                    {
                        var appInfo = await GetAppInfoAsync((uint)app.appid);
                        appInfo.MinutesPlayed2Weeks = app.playtime_2weeks;
                    }
                }
            });
        }

        private async Task BulkLoadAppInfoAsync(List<uint> appIds)
        {
            var initialAppIdLoadTimer = Stopwatch.StartNew();

            var filteredAppIds = appIds.Where(e => !LoadedAppInfos.ContainsKey(e))
                                                .Where(e => _licenseManager.AccountHasAppAccess(e))
                                                .Distinct()
                                                .ToList();

            // Breaking into at most 10 concurrent batches
            int batchSize = (filteredAppIds.Count / 10) + 1;
            var batches = filteredAppIds.Chunk(batchSize).ToList();

            // Breaking the request into smaller batches that complete faster
            var batchJobs = new List<Task>();
            foreach (var batch in batches)
            {
                batchJobs.Add(AppInfoRequestAsync(batch.ToList()));
            }

            await Task.WhenAll(batchJobs);
            _ansiConsole.LogMarkupVerbose($"Loaded metadata for {Magenta(filteredAppIds.Count)} apps", initialAppIdLoadTimer);
        }

        /// <summary>
        /// Retrieves the latest AppInfo for multiple apps at the same time.  One large request containing multiple apps is significantly faster
        /// than multiple individual requests, as it seems that there is a minimum threshold for how quickly steam will return results.
        /// </summary>
        /// <param name="appIdsToLoad">The list of App Ids to retrieve info for</param>
        private async Task AppInfoRequestAsync(List<uint> appIdsToLoad)
        {
            if (appIdsToLoad.Empty())
            {
                return;
            }

            // Some apps will require an additional "access token" in order to retrieve their app metadata
            var accessTokensResponse = await _steam3Session.SteamAppsApi.PICSGetAccessTokens(appIdsToLoad, new List<uint>()).ToTask();
            var appTokens = accessTokensResponse.AppTokens;

            // Build out the requests
            var requests = new List<PICSRequest>();
            foreach (var appId in appIdsToLoad)
            {
                var request = new PICSRequest(appId);
                if (appTokens.ContainsKey(appId))
                {
                    request.AccessToken = appTokens[appId];
                }
                requests.Add(request);
            }

            // Finally request the metadata from steam
            var resultSet = await _steam3Session.SteamAppsApi.PICSGetProductInfo(requests, new List<PICSRequest>()).ToTask();

            List<PicsProductInfo> appInfos = resultSet.Results.SelectMany(e => e.Apps).Select(e => e.Value).ToList();
            foreach (var app in appInfos)
            {
                LoadedAppInfos.TryAdd(app.ID, new AppInfo(_steam3Session, app.ID, app.KeyValues));

                app.KeyValues.WriteDebugFiles($@"{AppConfig.DebugOutputDir}\AppInfo\AppInfo_{app.ID}.txt");
            }
        }

        /// <summary>
        /// Steam stores all DLCs for a game as separate "apps", so they must be loaded after the game's AppInfo has been retrieved,
        /// and the list of DLC AppIds are known.
        ///
        /// Once the DLC apps are loaded, the final combined depot list (both the app + dlc apps) will be built.
        /// </summary>
        private async Task FetchDlcAppInfoAsync()
        {
            var dlcAppIds = LoadedAppInfos.Values.SelectMany(e => e.DlcAppIds).ToList();
            var containingAppIds = LoadedAppInfos.Values.Where(e => e.Type == AppType.Game)
                                                 .SelectMany(e => e.Depots)
                                                 .Select(e => e.ContainingAppId)
                                                 .ToList();

            var idsToLoad = containingAppIds.Union(dlcAppIds).ToList();
            await BulkLoadAppInfoAsync(idsToLoad);

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

        #endregion

        /// <summary>
        /// Will return an AppInfo for the specified AppId, that contains various metadata about the app.
        /// If the information for the specified app hasn't already been retrieved, then a request to the Steam network will be made.
        /// </summary>
        public virtual async Task<AppInfo> GetAppInfoAsync(uint appId)
        {
            if (LoadedAppInfos.ContainsKey(appId))
            {
                return LoadedAppInfos[appId];
            }

            await AppInfoRequestAsync(new List<uint> { appId });
            return LoadedAppInfos[appId];
        }

        /// <summary>
        /// Gets a list of all games owned by the current user.
        /// This differs from the list of owned AppIds, as this exclusively contains "games", excluding things like DLC/Tools/etc
        /// </summary>
        private async Task<List<CPlayer_GetOwnedGames_Response.Game>> GetUsersOwnedGamesAsync()
        {
            if (_ownedGames != null)
            {
                return _ownedGames;
            }

            var request = new CPlayer_GetOwnedGames_Request
            {
                steamid = _steam3Session._steamId,
                include_appinfo = true,
                include_free_sub = true,
                include_played_free_games = true,
                skip_unvetted_apps = false
            };
            var response = await _steam3Session.unifiedPlayerService.SendMessage(e => e.GetOwnedGames(request)).ToTask();
            if (response.Result != EResult.OK)
            {
                throw new Exception("Unexpected error while requesting owned games!");
            }

            _ownedGames = response.GetDeserializedResponse<CPlayer_GetOwnedGames_Response>().games;
            return _ownedGames;
        }

        public async Task<List<CPlayer_GetOwnedGames_Response.Game>> GetRecentlyPlayedGamesAsync()
        {
            var userOwnedGames = await GetUsersOwnedGamesAsync();
            return userOwnedGames.Where(e => e.playtime_2weeks > 0).ToList();
        }

        //TODO is this necessary because --all includes things that shouldn't be downloaded?
        /// <summary>
        /// Gets a list of available games, filtering out any unavailable, non-Windows games.
        /// </summary>
        public async Task<List<AppInfo>> GetAvailableGamesByIdAsync(List<uint> appIds)
        {
            //TODO maybe a call to retreive app metadata async here so that you don't need to remember to do it manually
            var appInfos = new List<AppInfo>();
            foreach (var appId in appIds)
            {
                appInfos.Add(await GetAppInfoAsync(appId));
            }

            // Filtering down some exclusions
            var excludedAppIds = Enum.GetValues(typeof(ExcludedAppId)).Cast<uint>().ToList();
            var filteredGames = appInfos.Where(e => (e.Type == AppType.Game || e.Type == AppType.Beta)
                                                    && (e.ReleaseState != ReleaseState.Unavailable && e.ReleaseState != ReleaseState.Prerelease)
                                                    && e.SupportsWindows
                                                    && _steam3Session.LicenseManager.AccountHasAppAccess(e.AppId))
                                        .Where(e => !excludedAppIds.Contains(e.AppId))
                                        .Where(e => !e.Categories.Contains(Category.Mods) && !e.Categories.Contains(Category.ModsHL2))
                                        .Where(e => !e.Name.Contains("AMD Driver Updater"))
                                        .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                                        .ToList();

            return filteredGames;
        }
    }
}