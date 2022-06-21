using System.Linq;
using SteamKit2;

namespace DepotDownloader.Models
{
    public class CommonInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public uint GameID { get; set; }

        public CommonInfo()
        {
            // Parameterless constructor for deserialization
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
