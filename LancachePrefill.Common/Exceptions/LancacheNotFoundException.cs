namespace LancachePrefill.Common.Exceptions
{
    [Serializable]
    public class LancacheNotFoundException : Exception
    {
        protected LancacheNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

        public LancacheNotFoundException()
        {

        }

        public LancacheNotFoundException(string message) : base(message)
        {

        }

        public LancacheNotFoundException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}