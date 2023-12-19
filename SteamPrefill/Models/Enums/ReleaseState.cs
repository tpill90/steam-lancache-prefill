namespace SteamPrefill.Models.Enums
{
    /// <summary>
    /// Steam docs :
    /// https://partner.steamgames.com/doc/api/steam_api?#EAppReleaseState
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    [Intellenum(typeof(string))]
    public sealed partial class ReleaseState
    {
        /// <summary>
        /// Unknown, can't get application information, or license info is missing.
        /// </summary>
        public static readonly ReleaseState Unknown = new("unknown");

        /// <summary>
        /// Even if user owns it, they can't see game at all.
        /// </summary>
        public static readonly ReleaseState Unavailable = new("unavailable");

        /// <summary>
        /// Can be purchased and is visible in games list, nothing else.
        /// </summary>
        public static readonly ReleaseState Prerelease = new("prerelease");

        /// <summary>
        /// Owners can preload app, not play it.
        /// </summary>
        public static readonly ReleaseState PreloadOnly = new("preloadonly");

        /// <summary>
        /// Owners can download and play app.
        /// </summary>
        public static readonly ReleaseState Released = new("released");

        public static readonly ReleaseState Disabled = new("disabled");
    }
}