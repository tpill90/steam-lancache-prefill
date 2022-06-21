using System;

namespace DepotDownloader.Models
{
    public class ContentDownloaderException : Exception
    {
        public ContentDownloaderException(string value) : base(value) { }
    }
}