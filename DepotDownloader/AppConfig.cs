using AutoMapper;
using DepotDownloader.Models;
using SteamKit2.CDN;

namespace DepotDownloader
{
    //TODO document
    public class AppConfig
    {
        public static string ManifestCacheDir => "ManifestCache";
        public static string ConfigDir => ".DepotDownloader";

        public static string AccountSettingsStorePath => "account.config";

        //TODO doccument that this is the user's region
        public static int CellID = 0;
        
        public static Mapper AutoMapper = new Mapper(new MapperConfiguration(cfg =>
                cfg.CreateMap<ServerShim, Server>()
        ));

        #region Debug

        //TODO revert
        public static bool SkipDownload = true;

        #endregion
    }
}
