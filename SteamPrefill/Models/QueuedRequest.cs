namespace SteamPrefill.Models
{
    [ProtoContract(SkipConstructor = true)]
    public struct QueuedRequest
    {
        [ProtoMember(1)]
        public readonly uint DepotId;

        /// <summary>
        /// The SHA-1 hash of the chunk's id.
        /// </summary>
        [ProtoMember(2)]
        public string ChunkId;

        /// <summary>
        /// The content-length of the data to be requested, in bytes.
        /// </summary>
        [ProtoMember(3)]
        public readonly uint CompressedLength;

        public Exception LastFailureReason { get; set; }

        public QueuedRequest(Manifest depotManifest, ChunkData chunk)
        {
            DepotId = depotManifest.DepotId;
            ChunkId = chunk.ChunkId;
            CompressedLength = chunk.CompressedLength;
        }

        public override string ToString()
        {
            return $"{DepotId} - {ChunkId}";
        }
    }
}