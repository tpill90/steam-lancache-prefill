using System;
using System.Collections.Generic;
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

        public uint Version { get; set; }

        [UsedImplicitly]
        public AppInfoShim()
        {
            // Parameterless constructor for deserialization
        }

        public AppInfoShim(uint appId, uint version, KeyValue rootKeyValues)
        {
            var c = rootKeyValues.Children;

            AppId = appId;
            Version = version;

            Common = new CommonInfo(c.FirstOrDefault(e => e.Name == "common"));
            Depots = BuildDepotInfos(c.FirstOrDefault(e => e.Name == "depots"));
        }

        private List<DepotInfo> BuildDepotInfos(KeyValue depotsRootKey)
        {
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
