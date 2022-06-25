using System;
using DepotDownloader.Protos;

namespace DepotDownloader.Models
{
    public class QueuedRequest
    {
        public ProtoManifest.ChunkData chunk;
        public DepotInfo depotDownloadInfo;

        public Exception PreviousError;
    }
}
