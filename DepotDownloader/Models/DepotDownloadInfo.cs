namespace DepotDownloader.Models
{
    //TODO document
    public sealed class DepotDownloadInfo
    {
        public uint id { get; set; }
        public uint appId { get; set; }
        public ulong manifestId { get; set; }

        public string contentName { get; set; }

        public byte[] depotKey { get; set; }

        public DepotDownloadInfo(uint depotid, uint appId, ulong manifestId, string contentName)
        {
            this.id = depotid;
            this.appId = appId;
            this.manifestId = manifestId;
            this.contentName = contentName;
        }
    }
}