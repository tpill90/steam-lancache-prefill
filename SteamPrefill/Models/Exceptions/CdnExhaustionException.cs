namespace SteamPrefill.Models.Exceptions
{
    public sealed class CdnExhaustionException : Exception
    {
        public CdnExhaustionException(string message) : base(message)
        {

        }
    }
}