using System;
using System.Runtime.Serialization;

namespace SteamPrefill.Models.Exceptions
{
    [Serializable]
    public class ManifestException : Exception
    {
        protected ManifestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }

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