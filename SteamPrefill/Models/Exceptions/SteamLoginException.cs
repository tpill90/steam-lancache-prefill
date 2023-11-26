namespace SteamPrefill.Models.Exceptions
{
    public sealed class SteamLoginException : Exception
    {
        public SteamLoginException()
        {

        }

        public SteamLoginException(string message) : base(message)
        {

        }

        public SteamLoginException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}