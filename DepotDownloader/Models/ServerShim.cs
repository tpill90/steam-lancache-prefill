
using SteamKit2.CDN;

namespace DepotDownloader.Models
{
    public class ServerShim
    {
        public uint CellID { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        public Server.ConnectionProtocol Protocol { get; set; }

        public string VHost { get; set; }
    }
}
