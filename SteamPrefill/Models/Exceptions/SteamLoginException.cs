namespace SteamPrefill.Models.Exceptions
{
    public sealed class SteamLoginException : Exception
    {
        public SteamLoginException(string message) : base(message)
        {

        }
    }
}