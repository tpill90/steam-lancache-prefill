namespace SteamPrefill.Models.Exceptions
{
    public sealed class SteamConnectionException : Exception
    {
        public SteamConnectionException()
        {

        }

        public SteamConnectionException(string message) : base(message)
        {

        }

        public SteamConnectionException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}