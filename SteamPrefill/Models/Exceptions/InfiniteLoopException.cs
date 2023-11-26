namespace SteamPrefill.Models.Exceptions
{
    public sealed class InfiniteLoopException : Exception
    {
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