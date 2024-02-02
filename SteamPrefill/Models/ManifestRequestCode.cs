namespace SteamPrefill.Models
{
    /// <summary>
    /// Requests a ManifestRequestCode for the specified depot.  Each depot will have a unique code, that gets rotated every 5 minutes.
    /// These manifest codes are not unique to a user, so they will be used by all users in the same 5 minute window.
    ///
    /// These manifest codes act as a form of "authorization" for the CDN.  You can only download a manifest if your account has access to the
    /// specified depot, so since the CDN itself doesn't check for access, this will prevent unauthorized depot downloads
    ///
    /// https://steamdb.info/blog/manifest-request-codes/ 
    /// </summary>
    public sealed class ManifestRequestCode
    {
        public ulong Code { get; set; }
        public DateTime RetrievedAt { get; set; }

        public override string ToString()
        {
            return Code.ToString();
        }
    }
}
