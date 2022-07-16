namespace DepotDownloader
{
    //TODO document
    //TODO merge with download config
    public class DownloadArguments
    {
        /* TODO
                Need to validate all the possible values for this? Assuming it is windows/macos/linux.  But might be different than expected
                User input should be validated.
                Default behavior should be to download windows, or the current operating system
        */
        public string OperatingSystem { get; set; } = "windows";
        
        public bool DownloadAllPlatforms { get; set; }

        //TODO enum
        public string Architecture { get; set; } = "64";

        //TODO enum
        public string Language { get; set; } = "english";

        public bool DownloadAllLanguages { get; set; }

        public bool LowViolence { get; set; }

        public bool Force { get; set; }
    }
}
