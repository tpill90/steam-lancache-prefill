namespace SteamPrefill.Models.Enums
{
    /// <summary>
    /// Steam docs :
    /// https://partner.steamgames.com/doc/api/steam_api?language=english#EAppReleaseState
    /// </summary>
    public class ReleaseState : EnumBase<ReleaseState>
    {
        public static readonly ReleaseState Unknown = new ReleaseState("unknown");
        public static readonly ReleaseState Unavailable = new ReleaseState("unavailable");
        public static readonly ReleaseState Prerelease = new ReleaseState("prerelease");
        public static readonly ReleaseState PreloadOnly = new ReleaseState("preloadonly");
        public static readonly ReleaseState Released = new ReleaseState("released");

        private ReleaseState(string name) : base(name)
        {
        }
    }
}
