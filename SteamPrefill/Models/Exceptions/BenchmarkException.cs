namespace SteamPrefill.Models.Exceptions
{
    public sealed class BenchmarkException : Exception
    {
        public BenchmarkException()
        {

        }

        public BenchmarkException(string message) : base(message)
        {

        }

        public BenchmarkException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}