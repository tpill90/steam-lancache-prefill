using System.Collections.Generic;

namespace DepotDownloader.Models
{
    //TODO rename
    public class PackageInfoShim
    {
        public List<uint> AppIds { get; set; } = new List<uint>();

        public List<uint> DepotIds { get; set; } = new List<uint>();
    }
}
