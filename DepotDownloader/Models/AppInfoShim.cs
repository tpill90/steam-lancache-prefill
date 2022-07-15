using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SteamKit2;

namespace DepotDownloader.Models
{
    //TODO document
    //TODO extended properties includes the list of available languages for a game
    //TODO cleanup this class.  its a mess
    //TODO rename
    public class AppInfoShim
    {
        public uint AppId { get; set; }
        //TODO enum?
        public string State { get; set; }
        public uint Version { get; set; }
        public uint BuildId { get; set; }

        //TODO filter this down into "owned" dlc app ids
        public List<uint> DlcAppIds { get; set; } = new List<uint>();

        /// <summary>
        /// Includes this app's depots, as well as any depots from its "children" DLC apps
        /// </summary>
        public List<DepotInfo> Depots { get; set; }
        
        public CommonInfo Common { get; set; }
        
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

            // Depots
            var depotSection = c.FirstOrDefault(e => e.Name == "depots");
            if (depotSection != null)
            {
                Depots = BuildDepotInfos(depotSection);

                //TODO cleanup
                var branchesSection = depotSection.Children.FirstOrDefault(e => e.Name == "branches");
                var publicSection = branchesSection?.Children.FirstOrDefault(e => e.Name == "public");
                if (publicSection != null)
                {
                    BuildId = UInt32.Parse(publicSection.Children.FirstOrDefault(e => e.Name == "buildid").Value);
                }
                
            }

            // Extended
            var extendedSection = c.FirstOrDefault(e => e.Name == "extended");
            if (extendedSection != null)
            {
                State = extendedSection.Children.FirstOrDefault(e => e.Name == "state")?.Value;

                var listOfDlc = extendedSection.Children.FirstOrDefault(e => e.Name == "listofdlc");
                if (listOfDlc != null)
                {
                    DlcAppIds = listOfDlc.Value.Split(",").Select(e => UInt32.Parse(e)).ToList();
                }
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

        public override string ToString()
        {
            return $"{AppId} - {Common?.Name}";
        }
    }
}
