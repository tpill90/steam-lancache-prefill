using AutoMapper;
using DepotDownloader.Models;
using SteamKit2.CDN;

namespace DepotDownloader
{
    public class DownloadConfig
    {
        public int CellID { get; set; }
        public bool DownloadAllPlatforms { get; set; }
        public bool DownloadAllLanguages { get; set; }

        public string SuppliedPassword { get; set; }
        public bool RememberPassword { get; set; }

        //TODO split out into AppConfig
        public static string ManifestCacheDir => "ManifestCache";
        public static string ConfigDir => ".DepotDownloader";

        // Debugging only
        //TODO revert
        public static bool SkipDownload = true;

        //TODO split out into AppConfig
        public static Mapper AutoMapper = new Mapper(new MapperConfiguration(cfg =>
                cfg.CreateMap<ServerShim, Server>()
        ));
    }
}
