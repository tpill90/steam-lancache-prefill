namespace SteamPrefill.Models
{
    //TODO document
    public class DownloadArguments
    {
        public bool Force { get; set; }

        //TODO reimplement these flags
        // Unimplemented
        public string OperatingSystem { get; set; } = "windows";
        public bool DownloadAllPlatforms { get; set; }
        //TODO enum
        public string Architecture { get; set; } = "64";
        //TODO enum
        public string Language { get; set; } = "english";
        public bool DownloadAllLanguages { get; set; }
        public bool LowViolence { get; set; }
    }
}
