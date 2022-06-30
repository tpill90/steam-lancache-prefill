using System;

namespace DepotDownloader.Models
{
    //TODO document
    public class ManifestRequestCode
    {
        // The manifest request code is only valid for a specific period in time
        public ulong Code { get; set; }

        public DateTime RetrievedAt { get; set; }
    }
}
