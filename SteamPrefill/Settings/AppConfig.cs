namespace SteamPrefill.Settings
{
    public static class AppConfig
    {
        static AppConfig()
        {
            // Create required folders
            Directory.CreateDirectory(ConfigDir);
            Directory.CreateDirectory(CacheDir);
        }

#if DEBUG

        public static bool EnableSteamKitDebugLogs => false;
        public static bool SkipDownloads { get; set; }

#endif

        //TODO comment
        public static bool VerboseLogs { get; set; }

        public static string SteamCdnUrl => "lancache.steamcontent.com";

        /// <summary>
        /// Downloaded manifests, as well as other metadata, are saved into this directory to speedup future prefill runs.
        /// All data in here should be able to be deleted safely.
        /// </summary>
        //TODO consider moving this to the user's directory, so that the self contained directory is much smaller
        public static readonly string CacheDir = Path.Combine(AppContext.BaseDirectory, "Cache", "v4");

        /// <summary>
        /// Contains user configuration.  Should not be deleted, doing so will reset the app back to defaults.
        /// </summary>
        public static readonly string ConfigDir = Path.Combine(AppContext.BaseDirectory, "Config");

        #region Serialization file paths

        public static readonly string AccountSettingsStorePath = Path.Combine(ConfigDir, "account.config");

        //TODO document what this is for
        public static readonly string BenchmarkWorkloadPath = Path.Combine(ConfigDir, "benchmarkWorkload.bin");
        public static readonly string UserSelectedAppsPath = Path.Combine(ConfigDir, "selectedAppsToPrefill.json");

        #endregion
    }
}