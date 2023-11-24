namespace SteamPrefill.Models.Exceptions
{
    public sealed class CdnExhaustionException : Exception
    {
        public CdnExhaustionException()
        {

        }

        public CdnExhaustionException(string message) : base(message)
        {

        }

        public CdnExhaustionException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}