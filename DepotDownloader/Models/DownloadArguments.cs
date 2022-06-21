
namespace DepotDownloader.Models
{
    //TODO document
    public class DownloadArguments
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public uint AppId { get; set; }

        /* TODO
                Need to validate all the possible values for this? Assuming it is windows/macos/linux.  But might be different than expected
                User input should be validated.
                Default behavior should be to download windows, or the current operating system
        */
        public string OperatingSystem { get; set; } = "windows";
        public string Architecture { get; set; }

        public string Language { get; set; } = "english";

        public bool LowViolence { get; set; }
    }
}
