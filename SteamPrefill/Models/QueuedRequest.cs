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

        /// <summary>
        /// Adler-32 hash, always 4 bytes
        /// </summary>
        [ProtoMember(4)]
        public readonly uint ExpectedChecksum;

        //TODO remove?
        [ProtoMember(5)]
        public readonly string ExpectedChecksumString;

        [ProtoMember(6)]
        public readonly byte[] DepotKey;

        public ChunkData chunkData;

		public Exception LastFailureReason { get; set; }

        public QueuedRequest(Manifest depotManifest, ChunkData chunk, byte[] depotKey)
        {
            DepotId = depotManifest.DepotId;
            ChunkId = chunk.ChunkId;
            CompressedLength = chunk.CompressedLength;

            ExpectedChecksum = chunk.Checksum;
            ExpectedChecksumString = chunk.ChecksumString;

            DepotKey = depotKey;
            chunkData = chunk;
        }

        public DepotManifest.ChunkData ToChunkData()
        {
            return new DepotManifest.ChunkData(chunkData.ChunkIDOriginal, ExpectedChecksum, chunkData.Offset, CompressedLength, chunkData.UncompressedLength);
        }

        public override string ToString()
        {
            return $"{DepotId} - {ChunkId}";
        }
    }
}