namespace SteamPrefill.Models.Enums
{
    public class OperatingSystem : EnumBase<OperatingSystem>
    {
        public static readonly OperatingSystem Windows = new OperatingSystem("windows");
        public static readonly OperatingSystem MacOS = new OperatingSystem("macos");
        public static readonly OperatingSystem Linux = new OperatingSystem("linux");

        private OperatingSystem(string name) : base(name)
        {
        }
    }
}
