using System;
using System.Linq;
using SteamKit2;

namespace DepotDownloader.Models
{
    //TODO document
    public class DepotInfo
    {
        public uint DepotId { get; set; }

        public string Name { get; set; }

        public ConfigInfo ConfigInfo { get; set; }

        //TODO this may need to be checked to see if it has changed, otherwise there is no way to know if there is an update
        public ulong ManifestId { get; set; } = DownloadConfig.INVALID_MANIFEST_ID;

        public long MaxSize { get; set; }
        public uint? DepotFromApp { get; set; } = null;

        public DepotInfo()
        {
            // Parameterless constructor for deserialization
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
            var depotFromApp = c.FirstOrDefault(e => e.Name == "depotfromapp");
            if (depotFromApp != null)
            {
                DepotFromApp = UInt32.Parse(c.First(e => e.Name == "depotfromapp").Value);
            }
        }

        public override string ToString()
        {
            return $"{DepotId} - {Name}";
        }
    }
}
