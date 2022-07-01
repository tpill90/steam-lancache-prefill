using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using SteamKit2;

namespace DepotDownloader.Models
{
    //TODO document
    public class DepotInfo
    {
        public uint DepotId { get; set; }

        public string Name { get; set; }

        public ConfigInfo ConfigInfo { get; set; }

        public ulong? ManifestId { get; set; }

        public long MaxSize { get; set; }

        public uint ContaingAppId { get; set; }
        public uint? DepotFromApp { get; set; }

        public int? LvCache { get; set; }

        //TODO comment
        public string ManifestFileName => $"{AppConfig.ManifestCacheDir}\\{ContaingAppId}_{DepotId}_{ManifestId}.bin";

        [UsedImplicitly]
        public DepotInfo()
        {
            // Parameter-less constructor for deserialization
        }

        public DepotInfo(KeyValue keyValues)
        {
            var c = keyValues.Children;

            DepotId = uint.Parse(keyValues.Name);
            Name = c.FirstOrDefault(e => e.Name == "name")?.Value;

            // Config
            var configSection = c.FirstOrDefault(e => e.Name == "config");
            if (configSection != null)
            {
                ConfigInfo = new ConfigInfo(configSection);
            }
            
            // MaxSize
            var maxSizeSection = c.FirstOrDefault(e => e.Name == "maxsize");
            if (maxSizeSection != null)
            {
                MaxSize = long.Parse(maxSizeSection.Value);
            }
            
            // ManifestId
            var manifestIdString = c.FirstOrDefault(e => e.Name == "manifests")
                                        ?.Children.FirstOrDefault(e => e.Name == "public");
            if (manifestIdString != null)
            {
                ManifestId = ulong.Parse(manifestIdString.Value);
            }

            // DepotFromApp
            var depotFromAppSection = c.FirstOrDefault(e => e.Name == "depotfromapp");
            if (depotFromAppSection != null)
            {
                DepotFromApp = UInt32.Parse(depotFromAppSection.Value);
            }

            // lvcache
            var lvcache = c.FirstOrDefault(e => e.Name == "lvcache");
            if (lvcache != null)
            {
                LvCache = Int32.Parse(lvcache.Value);
            }
        }

        public override string ToString()
        {
            return $"{DepotId} - {Name}";
        }
    }
}
