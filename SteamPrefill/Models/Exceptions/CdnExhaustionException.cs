namespace SteamPrefill.Models.Exceptions
{
    [Serializable]
    public class CdnExhaustionException : Exception
    {
        protected CdnExhaustionException(SerializationInfo info, StreamingContext context) : base(info, context)
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