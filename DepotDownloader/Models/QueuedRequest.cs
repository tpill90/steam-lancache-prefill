using System;
using DepotDownloader.Protos;

namespace DepotDownloader.Models
{
    public class QueuedRequest
    {
        public ProtoManifest.ChunkData chunk;
        public DepotDownloadInfo depotDownloadInfo;

        public Exception PreviousError;
    }
}
