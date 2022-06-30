using SteamKit2.CDN;

namespace DepotDownloader.Models
{
    //TODO rename
    public class ServerShim
    {
        public string Host { get; set; }
        public string VHost { get; set; }

        public int Port { get; set; }
        public Server.ConnectionProtocol Protocol { get; set; }

        //TODO document
        public Server ToSteamKitServer()
        {
            return AppConfig.AutoMapper.Map<Server>(this);
        }
    }
}
