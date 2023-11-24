namespace SteamPrefill.Models
{
    /// <summary>
    /// Represents an application (game, tool, video, server) that can be downloaded from steam
    /// </summary>
    public sealed class AppInfo
    {
        public uint AppId { get; set; }
        public ReleaseState ReleaseState { get; set; }

        /// <summary>
        /// Games on Steam can potentially have multiple "release dates", and are not consistently populated across all games.
        /// Determines which date to use based on which ones are currently populated.
        /// </summary>
        public DateTime? ReleaseDate
        {
            get
            {
                if (OriginalReleaseDate != null)
                {
                    return OriginalReleaseDate;
                }
                if (SteamReleaseDate != null)
                {
                    return SteamReleaseDate;
                }
                // For some reason, some games are missing both release dates, with no other alternative dates in their KeyValue pairs.
                // These games are even missing a release date in the Steam store.
                return null;
            }
        }

        private DateTime? SteamReleaseDate { get; set; }
        private DateTime? OriginalReleaseDate { get; set; }

        public List<uint> DlcAppIds { get; } = new List<uint>();

        /// <summary>
        /// Includes this app's depots, as well as any depots from its "children" DLC apps
        /// </summary>
        public List<DepotInfo> Depots { get; } = new List<DepotInfo>();

        public string Name { get; set; }

        /// <summary>
        /// Lists Operating Systems supported by this app.  If there is no OS listed, then it is assumed Windows is supported by default
        /// </summary>
        public List<string> OSList { get; }

        // Some games simply don't have any OSList at all, so this means that they should always be considered as supported.
        public bool SupportsWindows => OSList.Empty() || OSList.Contains("windows");

        /// <summary>
        /// Specifies the type of app, can be "config", "tool", "game".  This seems to be up to the developer, and isn't 100% consistent.
        /// </summary>
        public AppType Type { get; }

        public bool IsInvalidApp => Type == null;

        public bool IsFreeGame { get; set; }

        public int? MinutesPlayed2Weeks { get; set; }

        public List<Category> Categories { get; init; }

        public AppInfo(Steam3Session steamSession, uint appId, KeyValue rootKeyValue)
        {
            AppId = appId;

            Name = rootKeyValue["common"]["name"].Value.EscapeMarkup();
            Type = rootKeyValue["common"]["type"].AsEnum<AppType>(toLower: true);
            OSList = rootKeyValue["common"]["oslist"].SplitCommaDelimited();

            SteamReleaseDate = rootKeyValue["common"]["steam_release_date"].AsDateTimeUtc();
            OriginalReleaseDate = rootKeyValue["common"]["original_release_date"].AsDateTimeUtc();
            ReleaseState = rootKeyValue["common"]["releasestate"].AsEnum<ReleaseState>(toLower: true);

            if (rootKeyValue["depots"] != KeyValue.Invalid)
            {
                // Depots should always have a numerical ID for their name. For whatever reason Steam also includes branches + other metadata
                // that we don't care about in here, which will be filtered out as they don't have a numerical ID
                Depots = rootKeyValue["depots"].Children.Where(e => uint.TryParse(e.Name, out _))
                                                       .Select(e => new DepotInfo(e, appId))
                                                       .Where(e => !e.IsInvalidDepot)
                                                       .ToList();
            }

            // Extended Section
            IsFreeGame = rootKeyValue["extended"]["isfreeapp"].AsBoolean();
            var listOfDlc = rootKeyValue["extended"]["listofdlc"].Value;
            if (listOfDlc != null)
            {
                DlcAppIds = listOfDlc.Split(",")
                                     .Select(e => uint.Parse(e))
                                     // Only including DLC that we own
                                     .Where(e => steamSession.LicenseManager.AccountHasAppAccess(e))
                                     .ToList();
            }

            Categories = rootKeyValue["common"]["category"]
                         .Children
                         .Select(e => (Category)int.Parse(e.Name.Replace("category_", "")))
                         .ToList();
        }

        public override string ToString()
        {
            return $"{Name.EscapeMarkup()}";
        }
    }
}