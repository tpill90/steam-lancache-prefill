using System.IO;

namespace DepotDownloader.Settings
{
    //TODO document
    public static class AppConfig
    {
        public static string AccountSettingsStorePath => Path.Combine(ConfigDir, "account.config");

        /// <summary>
        /// Downloaded manifests are saved into this directory, to speedup future prefill runs
        /// </summary>
        public static string ManifestCacheDir => "ManifestCache";

        //TODO find usages of this, and use Path.Combine for cross platform compatibility
        public static string ConfigDir => "SteamPrefillConfig";

        #region Debug

        //TODO revert
        public static bool SkipDownload = false;

        #endregion
    }
}
