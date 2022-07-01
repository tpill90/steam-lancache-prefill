using System;

namespace DepotDownloader.Exceptions
{
    public class ContentDownloaderException : Exception
    {
        public ContentDownloaderException(string value) : base(value) { }
    }
}