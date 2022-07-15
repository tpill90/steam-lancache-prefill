using System.IO;

namespace SteamPrefill.Settings
{
    //TODO document
    public static class AppConfig
    {
        static AppConfig()
        {
            // Create required folders
            Directory.CreateDirectory(ConfigDir);
            Directory.CreateDirectory(CacheDir);
        }

        public static string AccountSettingsStorePath => $"{ConfigDir}/account.config";

        /// <summary>
        /// Downloaded manifests, as well as other metadata, are saved into this directory to speedup future prefill runs
        /// </summary>
        public static string CacheDir => "Cache";

        //TODO find usages of this, and use Path.Combine for cross platform compatibility
        public static string ConfigDir => "SteamPrefillConfig";
    }
}
