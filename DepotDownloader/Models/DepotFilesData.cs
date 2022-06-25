using System.Collections.Generic;
using DepotDownloader.Protos;

namespace DepotDownloader.Models
{
    public class DepotFilesData
    {
        public DepotInfo depotDownloadInfo;
       
        public List<ProtoManifest.FileData> filteredFiles;
    }
}