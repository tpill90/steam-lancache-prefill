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
        //TODO I wonder if this large number of concurrent requests is really necessary.  Test with Dota 2 + Destiny.  Test against https://speed.cloudflare.com/ to prove numbers improve
        public int MaxConcurrentRequests { get; set; } = 50;

        public OperatingSystem OperatingSystem { get; set; } = OperatingSystem.Windows;
        public Architecture Architecture { get; set; } = Architecture.x64;
        public Language Language { get; set; } = Language.English;
    }
}