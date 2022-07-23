using System.Collections.Generic;
using System.Linq;
using SteamPrefill.Settings;
using JetBrains.Annotations;
using SteamKit2;
using SteamPrefill.Models.Enum;
using SteamPrefill.Utils;
using OperatingSystem = SteamPrefill.Models.Enum.OperatingSystem;

namespace SteamPrefill.Models
{
    //TODO document what these fields do.  Not all of them are obvious
    //TODO setters should be private
    public class DepotInfo
    {
        public uint DepotId { get; set; }
        public string Name { get; set; }

        public ulong? ManifestId { get; set; }
        public uint? DepotFromApp { get; set; }

        // If there is no manifest we can't download this depot, and if there is no shared depot then we can't look up a related manifest we could use
        public bool IsInvalidDepot => ManifestId == null && DepotFromApp == null;

        public uint ContainingAppId { get; set; }
        public uint? DlcAppId { get; set; }

        public List<OperatingSystem> SupportedOperatingSystems { get; set; } = new List<OperatingSystem>();
        public Architecture Architecture { get; set; }
        public List<Language> Languages { get; set; }
        public bool? LowViolence { get; set; }

        public string ManifestFileName => $"{AppConfig.CacheDir}/{ContainingAppId}_{DepotId}_{ManifestId}.bin";

        [UsedImplicitly]
        public DepotInfo()
        {
            // Parameter-less constructor for deserialization
        }

        public DepotInfo(KeyValue rootKey, uint appId)
        {
            DepotId = uint.Parse(rootKey.Name);
            Name = rootKey["name"].Value;

            ManifestId = rootKey["manifests"]["public"].AsUnsignedLongNullable();
            DepotFromApp = rootKey["depotfromapp"].AsUnsignedIntNullable();
            DlcAppId = rootKey["dlcappid"].AsUnsignedIntNullable();

            // Config Section
            if (rootKey["config"]["oslist"] != KeyValue.Invalid)
            {
                SupportedOperatingSystems = rootKey["config"]["oslist"].Value
                                                                         .Split(',')
                                                                         .Select(e => OperatingSystem.Parse(e))
                                                                         .ToList();
            }
            Architecture = rootKey["config"]["osarch"].AsEnum<Architecture>();
            
            Languages = rootKey["config"]["language"].SplitCommaDelimited()
                                                    .Select(e => Language.Parse(e))
                                                    .ToList();

            if (rootKey["config"]["lowviolence"].Value is "1")
            {
                LowViolence = true;
            }

            //TODO comment
            ContainingAppId = appId;
            if (DlcAppId != null)
            {
                ContainingAppId = DlcAppId.Value;
            }
            if (DepotFromApp != null)
            {
                ContainingAppId = DepotFromApp.Value;
            }
        }

        public override string ToString()
        {
            return $"{DepotId} - {Name}";
        }
    }
}
