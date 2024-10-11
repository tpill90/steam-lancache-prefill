namespace SteamPrefill.Models
{
    public sealed class DownloadArguments
    {
        private int _maxConcurrentRequests = 30;

        /// <summary>
        /// When set to true, always run the download, regardless of if the app has been previously downloaded.
        /// </summary>
        public bool Force { get; init; }

        /// <summary>
        /// Determines which unit to display the download speed in.
        /// </summary>
        public TransferSpeedUnit TransferSpeedUnit { get; set; } = TransferSpeedUnit.Bits;

        /// <summary>
        /// Limits the maximum number of requests that can be in flight at any one time.  Does not guarantee there will always be 30,
        /// it is just an upper limit.
        ///
        /// The default of 30 was found to be a good middle ground for maximum throughput. It also minimizes the potential for SteamPrefill to
        /// choke out any other downloads on the network, without having to require users setup QoS themselves.
        /// </summary>
        public int MaxConcurrentRequests
        {
            get
            {
                //TODO I don't like how this is using a static variable like this, because nothing else in this class is like this. Consider removing this later once its no longer used for debugging.
                if (AppConfig.MaxConcurrencyOverride != null)
                {
                    return AppConfig.MaxConcurrencyOverride.Value;
                }
                return _maxConcurrentRequests;
            }
            set => _maxConcurrentRequests = value;
        }

        /// <summary>
        /// Determines which Operating System specific depots should be included in the download.
        /// </summary>
        public List<OperatingSystem> OperatingSystems { get; init; } = new List<OperatingSystem> { OperatingSystem.Windows };

        public Architecture Architecture { get; init; } = Architecture.x64;
        public Language Language { get; init; } = Language.English;
    }
}