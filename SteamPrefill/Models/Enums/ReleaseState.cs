namespace SteamPrefill.Models.Enums
{
    /// <summary>
    /// Steam docs :
    /// https://partner.steamgames.com/doc/api/steam_api?#EAppReleaseState
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public sealed class ReleaseState : EnumBase<ReleaseState>
    {
        /// <summary>
        /// Unknown, can't get application information, or license info is missing.
        /// </summary>
        public static readonly ReleaseState Unknown = new ReleaseState("unknown");

        /// <summary>
        /// Even if user owns it, they can't see game at all.
        /// </summary>
        public static readonly ReleaseState Unavailable = new ReleaseState("unavailable");

        /// <summary>
        /// Can be purchased and is visible in games list, nothing else.
        /// </summary>
        public static readonly ReleaseState Prerelease = new ReleaseState("prerelease");

        /// <summary>
        /// Owners can preload app, not play it.
        /// </summary>
        public static readonly ReleaseState PreloadOnly = new ReleaseState("preloadonly");

        /// <summary>
        /// Owners can download and play app.
        /// </summary>
        public static readonly ReleaseState Released = new ReleaseState("released");

        public static readonly ReleaseState Disabled = new ReleaseState("disabled");

        private ReleaseState(string name) : base(name)
        {
        }
    }
}
