using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Spectre.Console;
using SteamKit2;
using SteamPrefill.Handlers.Steam;
using SteamPrefill.Models.Enums;
using SteamPrefill.Utils;

namespace SteamPrefill.Models
{
    /// <summary>
    /// Represents an application (game, tool, video, server) that can be downloaded from steam
    /// </summary>
    public class AppInfo
    {
        public uint AppId { get; set; }
        public ReleaseState ReleaseState { get; set; }

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
        public AppType Type { get; set; }

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