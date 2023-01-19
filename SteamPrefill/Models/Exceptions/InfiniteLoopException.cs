namespace SteamPrefill.Models.Exceptions
{
    public class InfiniteLoopException : Exception
    {
        protected InfiniteLoopException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

        public InfiniteLoopException()
        {

        }

        public InfiniteLoopException(string message) : base(message)
        {

        }

        public InfiniteLoopException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}