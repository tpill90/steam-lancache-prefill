using System;
using System.Runtime.Serialization;

namespace SteamPrefill.Models.Exceptions
{
    [Serializable]
    public class SteamLoginException : Exception
    {
        protected SteamLoginException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

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