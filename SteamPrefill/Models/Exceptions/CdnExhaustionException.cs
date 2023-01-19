namespace SteamPrefill.Models.Exceptions
{
    public class CdnExhaustionException : Exception
    {
        private CdnExhaustionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

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