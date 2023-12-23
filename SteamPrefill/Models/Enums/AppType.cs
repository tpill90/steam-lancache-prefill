namespace SteamPrefill.Models.Enums
{
    /// <summary>
    /// Steam docs:
    /// https://partner.steamgames.com/doc/api/steam_api?#EAppType
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    [Intellenum(typeof(string))]
    public sealed partial class AppType
    {
        public static readonly AppType Application = new AppType("application");
        public static readonly AppType Beta = new AppType("beta");
        public static readonly AppType Config = new AppType("config");
        public static readonly AppType Demo = new AppType("demo");
        public static readonly AppType Dlc = new AppType("dlc");
        public static readonly AppType Game = new AppType("game");
        public static readonly AppType Guide = new AppType("guide");
        public static readonly AppType Hardware = new AppType("hardware");
        public static readonly AppType Media = new AppType("media");
        public static readonly AppType Music = new AppType("music");
        public static readonly AppType Series = new AppType("series");
        public static readonly AppType Tool = new AppType("tool");
        public static readonly AppType Video = new AppType("video");
    }
}