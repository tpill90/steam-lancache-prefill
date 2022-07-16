using DepotDownloader.Settings;
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
        
        public uint CellId { get; set; }

        /// <summary>
        /// Gets the load value associated with this server.
        /// </summary>
        public int Load { get; set; }

        /// <summary>
        /// Gets the weighted load.
        /// </summary>
        public float WeightedLoad { get; set; }

        //TODO document
        public Server ToSteamKitServer()
        {
            return AppConfig.AutoMapper.Map<Server>(this);
        }

        public override string ToString()
        {
            return $"{Host} - Cell: {CellId} Load: {Load} - Weighted Load: {WeightedLoad}";
        }
    }
}
