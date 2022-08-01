namespace SteamPrefill.Models.Enums
{
    public class AppType : EnumBase<AppType>
    {
        public static readonly AppType Application = new AppType("application");
        public static readonly AppType Beta = new AppType("beta");
        public static readonly AppType Config = new AppType("config");
        public static readonly AppType Demo = new AppType("demo");
        public static readonly AppType Dlc = new AppType("dlc");
        public static readonly AppType Game = new AppType("game");
        public static readonly AppType Music = new AppType("music");
        public static readonly AppType Series = new AppType("series");
        public static readonly AppType Tool = new AppType("tool");
        public static readonly AppType Video = new AppType("video");

        private AppType(string name) : base(name)
        {
        }
    }
}
