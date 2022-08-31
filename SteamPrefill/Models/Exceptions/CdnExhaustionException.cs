namespace SteamPrefill.Models.Exceptions
{
    //TODO Do I need to have [Serializable] on each exception?
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