namespace SteamPrefill.Models
{
    public sealed class DownloadArguments
    {
        private int _maxConcurrentRequests = 30;

        /// <summary>
        /// When set to true, always run the download, regardless of if the app has been previously downloaded.
        /// </summary>
        public bool Force { get; init; } = false;

        /// <summary>
        /// Determines which Operating System specific depots should be included in the download.
        /// </summary>
        public List<OperatingSystem> OperatingSystems { get; init; } = new List<OperatingSystem> { OperatingSystem.Windows };

        public Architecture Architecture { get; init; } = Architecture.x64;
        public Language Language { get; init; } = Language.English;
    }
}