namespace SteamPrefill.Models.Exceptions
{
    public sealed class ManifestException : Exception
    {
        public ManifestException()
        {

        }

        public ManifestException(string message) : base(message)
        {

        }

        public ManifestException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}