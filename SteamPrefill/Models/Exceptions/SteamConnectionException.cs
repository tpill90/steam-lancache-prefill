namespace SteamPrefill.Models.Exceptions
{
    public sealed class SteamConnectionException : Exception
    {
        public SteamConnectionException(string message) : base(message)
        {

        }
    }
}