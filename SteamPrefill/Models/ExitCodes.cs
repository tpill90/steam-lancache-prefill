namespace SteamPrefill.Models
{
    /// <summary>
    /// Exit codes with a specific meaning, so that scheduled jobs (systemd, cron, Docker, etc.) can react to them.
    /// Any other non-zero exit code indicates a general error.
    /// </summary>
    public static class ExitCodes
    {
        /// <summary>
        /// The saved Steam login token has expired or was revoked (for example because the password was changed),
        /// and the app is running non-interactively, so the user can't be prompted for their password.
        /// Run the app once interactively to log in again.
        /// </summary>
        public const int ReauthenticationRequired = 10;
    }
}
