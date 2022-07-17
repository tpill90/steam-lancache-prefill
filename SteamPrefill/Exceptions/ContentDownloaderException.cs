using System;

namespace SteamPrefill.Exceptions
{
    public class ContentDownloaderException : Exception
    {
        public ContentDownloaderException(string value) : base(value) { }
    }
}