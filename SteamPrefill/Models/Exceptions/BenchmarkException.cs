namespace SteamPrefill.Models.Exceptions
{
    public class BenchmarkException : Exception
    {
        protected BenchmarkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

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