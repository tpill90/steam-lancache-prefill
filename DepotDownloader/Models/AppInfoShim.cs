using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using SteamKit2;

namespace DepotDownloader.Models
{
    //TODO document
    //TODO extended properties includes the list of available languages for a game
    //TODO rename
    public class AppInfoShim
    {
        public uint AppId { get; set; }

        public List<DepotInfo> Depots { get; set; }

        public CommonInfo Common { get; set; }

        //TODO enum?
        public string State { get; set; }
        
        public uint Version { get; set; }

        [UsedImplicitly]
        public AppInfoShim()
        {
            // Parameter-less constructor for deserialization
        }

        public AppInfoShim(uint appId, uint version, KeyValue rootKeyValues)
        {
            var c = rootKeyValues.Children;

            AppId = appId;
            Version = version;

            var commonSection = c.FirstOrDefault(e => e.Name == "common");
            if (commonSection != null)
            {
                Common = new CommonInfo(commonSection);
            }
            var depotSection = c.FirstOrDefault(e => e.Name == "depots");
            if (depotSection != null)
            {
                Depots = BuildDepotInfos(depotSection);
            }
            var extendedSection = c.FirstOrDefault(e => e.Name == "extended");
            if (extendedSection != null)
            {
                State = extendedSection.Children.FirstOrDefault(e => e.Name == "state")?.Value;
            }
        }

        private List<DepotInfo> BuildDepotInfos(KeyValue depotsRootKey)
        {
            //TODO add the baselanguages to the depotinfo, and maybe write a command that can list the available languages for an app
            var depotInfos = new List<DepotInfo>();
            foreach (var entry in depotsRootKey.Children)
            {
                if (!UInt32.TryParse(entry.Name, out _))
                {
                    continue;
                }
                depotInfos.Add(new DepotInfo(entry));
            }
            return depotInfos;
        }
    }
}
