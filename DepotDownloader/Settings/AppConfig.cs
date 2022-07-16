using System.IO;
using AutoMapper;
using DepotDownloader.Models;
using SteamKit2.CDN;

namespace DepotDownloader.Settings
{
    //TODO document
    public class AppConfig
    {
        //TODO this doesn't get put into the right directory on linux
        public static string ManifestCacheDir => "ManifestCache";

        //TODO find usages of this, and use Path.Combine for cross platform compatibility
        public static string ConfigDir => "SteamPrefillConfig";

        public static string AccountSettingsStorePath => Path.Combine(ConfigDir, "account.config");

        //TODO doccument that this is the user's region
        public static int CellID = 0;

        public static Mapper AutoMapper = new Mapper(new MapperConfiguration(cfg =>
                cfg.CreateMap<ServerShim, Server>()
        ));

        #region Debug

        //TODO revert
        public static bool SkipDownload = false;

        #endregion
    }
}
