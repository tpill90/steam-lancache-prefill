namespace SteamPrefill.Models
{
    public sealed class DownloadArguments
    {
        /// <summary>
        /// When set to true, always run the download, regardless of if the app has been previously downloaded.
        /// </summary>
        public bool Force { get; init; }

        /// <summary>
        /// When set to true, will avoid saving as much data to disk as possible
        /// </summary>
        public bool NoCache { get; set; }

        /// <summary>
        /// Determines which unit to display the download speed in.
        /// </summary>
        public TransferSpeedUnit TransferSpeedUnit { get; set; } = TransferSpeedUnit.Bits;

        //TODO comment
        public int MaxConcurrentRequests { get; set; } = 50;

        //TODO comment
        public List<OperatingSystem> OperatingSystems { get; set; } = new List<OperatingSystem> { OperatingSystem.Windows };

        public Architecture Architecture { get; init; } = Architecture.x64;
        public Language Language { get; init; } = Language.English;
    }
}