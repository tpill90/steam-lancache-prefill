namespace SteamPrefill.Settings
{
    //TODO document
    public static class AppConfig
    {
        public static string AccountSettingsStorePath => $"{ConfigDir}/account.config";

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
