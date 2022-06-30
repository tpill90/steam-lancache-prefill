using AutoMapper;
using DepotDownloader.Models;
using SteamKit2.CDN;

namespace DepotDownloader
{
    //TODO document
    public class AppConfig
    {
        public string SuppliedPassword { get; set; }
        public bool RememberPassword { get; set; }

        public static string ManifestCacheDir => "ManifestCache";
        public static string ConfigDir => ".DepotDownloader";
        
        public static Mapper AutoMapper = new Mapper(new MapperConfiguration(cfg =>
                cfg.CreateMap<ServerShim, Server>()
        ));

        #region Debug

        //TODO revert
        public static bool SkipDownload = false;

        #endregion
    }
}
