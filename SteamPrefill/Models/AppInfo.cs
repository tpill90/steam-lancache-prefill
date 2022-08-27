namespace SteamPrefill.Models
{
    /// <summary>
    /// Represents an application (game, tool, video, server) that can be downloaded from steam
    /// </summary>
    public class AppInfo
    {
        //TODO remove
        public bool IsSelected { get; set; }

        public uint AppId { get; set; }
        public ReleaseState ReleaseState { get; set; }

        //TODO comment
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
                return null;
            }
        }

        public DateTime? SteamReleaseDate { get; set; }
        public DateTime? OriginalReleaseDate { get; set; }

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
        public bool SupportsWindows => !OSList.Any() || OSList.Contains("windows");

        /// <summary>
        /// Specifies the type of app, can be "config", "tool", "game".  This seems to be up to the developer, and isn't 100% consistent.
        /// </summary>
        public AppType Type { get; }

        public int? MinutesPlayed2Weeks { get; set; }
        public decimal? HoursPlayed2Weeks => MinutesPlayed2Weeks == null ? null : (decimal)MinutesPlayed2Weeks / 60;

        public List<Category> Categories { get; init; }

        [UsedImplicitly]
        public AppInfo()
        {
            // Parameter-less constructor for deserialization
        }

        public AppInfo(Steam3Session steamSession, uint appId, KeyValue rootKeyValue)
        {
            AppId = appId;

            Name = rootKeyValue["common"]["name"].Value;
            Type = rootKeyValue["common"]["type"].AsEnum<AppType>(toLower: true);
            OSList = rootKeyValue["common"]["oslist"].SplitCommaDelimited();
            //TODO alot of games are missing this
            SteamReleaseDate = rootKeyValue["common"]["steam_release_date"].AsDateTimeUtc();
            OriginalReleaseDate = rootKeyValue["common"]["original_release_date"].AsDateTimeUtc();
            ReleaseState = rootKeyValue["extended"]["releasestate"].AsEnum<ReleaseState>();
            
            if (rootKeyValue["depots"] != KeyValue.Invalid)
            {
                // Depots should always have a ID for their name.
                // For whatever reason Steam also includes branches + other metadata that we don't care about in here as well.
                Depots = rootKeyValue["depots"].Children.Where(e => uint.TryParse(e.Name, out _))
                                               .Select(e => new DepotInfo(e, appId))
                                               .Where(e => !e.IsInvalidDepot)
                                               .ToList();
            }

            // Extended Section
            var listOfDlc = rootKeyValue["extended"]["listofdlc"].Value;
            if (listOfDlc != null)
            {
                DlcAppIds = listOfDlc.Split(",")
                                     .Select(e => uint.Parse(e))
                                     // Only including DLC that we own
                                     .Where(e => steamSession.AccountHasAppAccess(e))
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