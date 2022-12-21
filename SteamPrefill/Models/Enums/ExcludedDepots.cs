namespace SteamPrefill.Models.Enums
{
    public static class ExcludedDepots
    {
        public static readonly HashSet<ulong> Ids = new HashSet<ulong>
        {
            // VTOL VR - https://steamdb.info/depot/1770480/
            // Manually excluding this depot as it is currently impossible to request a manifest code for it, thus it shouldn't be downloaded.
            // I'm currently unable to determine how the real Steam client determines to skip this depot.
            1770480
        };
    }
}
