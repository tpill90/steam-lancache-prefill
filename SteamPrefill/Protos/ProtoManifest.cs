using System;
using System.Collections.Generic;
using System.IO;
using SteamPrefill.Models;
using HexMate;
using ProtoBuf;
using SteamKit2;

namespace SteamPrefill.Protos
{
    //TODO cleanup + document
    [ProtoContract]
    public class ProtoManifest
    {
        private ProtoManifest()
        {
            Files = new List<FileData>();
        }

        public ProtoManifest(DepotManifest sourceManifest, DepotInfo depotInfo) : this()
        {
            sourceManifest.Files.ForEach(f => Files.Add(new FileData(f)));
            ID = depotInfo.ManifestId.Value;
            CreationTime = sourceManifest.CreationTime;
            DepotId = depotInfo.DepotId;
        }

        [ProtoContract]
        public class FileData
        {
            // Proto ctor
            private FileData()
            {
                Chunks = new List<ChunkData>();
            }

            public FileData(DepotManifest.FileData sourceData) : this()
            {
                sourceData.Chunks.ForEach(c => Chunks.Add(new ChunkData(c)));
                Flags = sourceData.Flags;
                TotalSize = sourceData.TotalSize;
                FileHash = sourceData.FileHash;
            }

            /// <summary>
            /// Gets the chunks that this file is composed of.
            /// </summary>
            [ProtoMember(1)]
            public List<ChunkData> Chunks { get; private set; }

            /// <summary>
            /// Gets the file flags
            /// </summary>
            [ProtoMember(2)]
            public EDepotFileFlag Flags { get; private set; }

            /// <summary>
            /// Gets the total size of this file.
            /// </summary>
            [ProtoMember(3)]
            public ulong TotalSize { get; private set; }

            /// <summary>
            /// Gets the hash of this file.
            /// </summary>
            [ProtoMember(4)]
            public byte[] FileHash { get; private set; }
        }

        [ProtoContract(SkipConstructor = true)]
        public class ChunkData
        {
            public ChunkData(DepotManifest.ChunkData sourceChunk)
            {
                ChunkID = HexMate.Convert.ToHexString(sourceChunk.ChunkID, HexFormattingOptions.Lowercase);
                CompressedLength = sourceChunk.CompressedLength;
            }

            /// <summary>
            /// Gets the SHA-1 hash chunk id.
            /// </summary>
            [ProtoMember(1)]
            public string ChunkID { get; private set; }

            /// <summary>
            /// Gets the compressed length of this chunk.
            /// </summary>
            [ProtoMember(2)]
            public uint CompressedLength { get; private set; }

            public override string ToString()
            {
                return ChunkID;
            }
        }

        [ProtoMember(1)]
        public List<FileData> Files { get; private set; }

        [ProtoMember(2)]
        public ulong ID { get; private set; }

        [ProtoMember(3)]
        public DateTime CreationTime { get; private set; }

        [ProtoMember(4)]
        public uint DepotId { get; private set; }

        public static ProtoManifest LoadFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            using var fs = File.Open(filename, FileMode.Open);
            return Serializer.Deserialize<ProtoManifest>(fs);
        }

        public void SaveToFile(string filename)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, this);

                ms.Seek(0, SeekOrigin.Begin);

                using (var fs = File.Open(filename, FileMode.Create))
                {
                    ms.CopyTo(fs);
                }
            }
        }
    }
}
