namespace DepotDownloader
{
    //TODO document
    //TODO merge with download config
    public class DownloadArguments
    {
        public string Username { get; set; }

        public uint AppId { get; set; }

        /* TODO
                Need to validate all the possible values for this? Assuming it is windows/macos/linux.  But might be different than expected
                User input should be validated.
                Default behavior should be to download windows, or the current operating system
        */
        public string OperatingSystem { get; set; } = "windows";
        public bool DownloadAllPlatforms { get; set; }

        public string Architecture { get; set; }

        public string Language { get; set; } = "english";
        public bool DownloadAllLanguages { get; set; }

        public bool LowViolence { get; set; }
    }
}
