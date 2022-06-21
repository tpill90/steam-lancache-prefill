using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace DepotDownloader.Models
{
    //TODO document
    public class AppInfoShim
    {
        public uint AppId { get; set; }

        public List<DepotInfo> Depots { get; set; }

        public CommonInfo Common { get; set; }

        // Parameterless constructor for deserialization
        public AppInfoShim()
        {

        }

        public AppInfoShim(KeyValue rootKeyValues)
        {
            var c = rootKeyValues.Children;

            AppId =  uint.Parse(c.FirstOrDefault(e => e.Name == "appid").Value);
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
