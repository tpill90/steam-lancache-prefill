using System.Linq;
using JetBrains.Annotations;
using SteamKit2;

namespace DepotDownloader.Models
{
    //TODO document
    public class CommonInfo
    {
        public string Name { get; set; }

        //TODO enum
        public string Type { get; set; }
        public uint GameID { get; set; }

        [UsedImplicitly]
        public CommonInfo()
        {
            // Parameter-less constructor for deserialization
        }

        public CommonInfo(KeyValue keyValues)
        {
            var c = keyValues.Children;

            // MaxSize
            Name = c.FirstOrDefault(e => e.Name == "name").Value;
            Type = c.FirstOrDefault(e => e.Name == "type").Value;
            GameID = uint.Parse(c.FirstOrDefault(e => e.Name == "gameid").Value);
        }

        public override string ToString()
        {
            return $"{Name} - {GameID}";
        }
    }
}
