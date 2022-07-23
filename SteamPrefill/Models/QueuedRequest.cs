namespace SteamPrefill.Models
{
    public class QueuedRequest
    {
        public uint DepotId { get; }
        public string ChunkId { get; }

        /// <summary>
        /// The content-length of the data to be requested.
        /// </summary>
        public uint CompressedLength { get; }

        public QueuedRequest(Manifest depotManifest, ChunkData chunk)
        {
            DepotId = depotManifest.DepotId;
            ChunkId = chunk.ChunkID;
            CompressedLength = chunk.CompressedLength;
        }
    }
}