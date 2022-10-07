namespace SteamPrefill.Models
{
    public sealed class QueuedRequest
    {
        public uint DepotId { get; }
        public string ChunkId { get; }

        /// <summary>
        /// The content-length of the data to be requested.
        /// </summary>
        public uint CompressedLength { get; }

        //TODO comment what this does + why its needed
        public int ChunkNum { get; }

        public QueuedRequest(Manifest depotManifest, ChunkData chunk, int chunkNum)
        {
            DepotId = depotManifest.DepotId;
            ChunkId = chunk.ChunkID;
            CompressedLength = chunk.CompressedLength;

            ChunkNum = chunkNum;
        }
    }
}