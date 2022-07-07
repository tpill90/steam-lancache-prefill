namespace DepotDownloader.Models
{
    //TODO document
    public class QueuedRequest
    {
        public uint DepotId { get; set; }
        public string ChunkID { get; set; }
        public uint CompressedLength { get; set; }

        public override string ToString()
        {
            return CompressedLength.ToString();
        }
    }
}