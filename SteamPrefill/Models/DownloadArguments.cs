using SteamPrefill.Models.Enums;

namespace SteamPrefill.Models
{
    public class DownloadArguments
    {
        /// <summary>
        /// When set to true, always run the download, regardless of if the app has been previously downloaded.
        /// </summary>
        public bool Force { get; init; }

        public OperatingSystem OperatingSystem { get; set; } = OperatingSystem.Windows;

        public Architecture Architecture { get; set; } = Architecture.x64;

        public Language Language { get; set; } = Language.English;
    }
}