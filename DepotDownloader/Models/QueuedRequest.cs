using System;
using DepotDownloader.Protos;

namespace DepotDownloader.Models
{
    public class QueuedRequest
    {
        public ProtoManifest.ChunkData chunk;
        public uint DepotId;

        public Exception PreviousError;
    }
}
