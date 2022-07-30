using System.Net;
using SteamPrefill.Models.Enums;

namespace SteamPrefill.Models
{
    public class DownloadArguments
    {
        /// <summary>
        /// When set to true, always run the download, regardless of if the app has been previously downloaded.
        /// </summary>
        public bool Force { get; init; }

        /// <summary>
        /// When specified, will manually override the system's DNS resolution, and directly resolve a Lancache to the specified IP.
        /// Can be used to prefill on the same machine that hosts the Lancache
        /// </summary>
        public IPAddress OverrideLancacheIp { get; init; }

        public OperatingSystem OperatingSystem { get; set; } = OperatingSystem.Windows;

        public Architecture Architecture { get; set; } = Architecture.x64;

        public Language Language { get; set; } = Language.English;
    }
}