namespace SteamPrefill.Models.Exceptions
{
    public sealed class InfiniteLoopException : Exception
    {
        public InfiniteLoopException(string message) : base(message)
        {

        }
    }
}