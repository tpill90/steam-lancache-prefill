namespace SteamPrefill.Models.Enums
{
    [Intellenum(typeof(string))]
    public sealed partial class OperatingSystem
    {
        public static readonly OperatingSystem Windows = new("windows");
        public static readonly OperatingSystem MacOS = new("macos");
        public static readonly OperatingSystem Linux = new("linux");
    }
}