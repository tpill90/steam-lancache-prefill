using System.Collections.Generic;
using DepotDownloader.Protos;

namespace DepotDownloader.Models
{
    public class DepotFilesData
    {
        public DepotDownloadInfo depotDownloadInfo;
       
        public List<ProtoManifest.FileData> filteredFiles;
    }
}