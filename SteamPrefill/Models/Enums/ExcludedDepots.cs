namespace SteamPrefill.Models.Enums
{
    public static class ExcludedDepots
    {
        public static readonly HashSet<ulong> Ids = new HashSet<ulong>
        {
            // Dota 2 Workshop Tools - https://steamdb.info/depot/381450
            // Steam somehow determines that this depot should not be installed alongside Dota 2, however there is no associated metadata
            // that indicates that this depot should not be downloaded.  This must be hardcoded in the real Steam client as well.
            381450,
            // CS2 Workshop Tools, also unclear how Valve is filtering this from installing normally
            2347779,

            // VTOL VR - https://steamdb.info/depot/1770480/
            // Manually excluding this depot as it is currently impossible to request a manifest code for it, thus it shouldn't be downloaded.
            // I'm currently unable to determine how the real Steam client determines to skip this depot.
            1770480,

            // CS2 excludes these for some unknown reason.  Might be related to "systemdefined"	on the DepotInfo
            731,
            732,
            734,
            
            // Excluding VC Redist since they add unnecessary noise to a app's depot list
            228981,
            228982,
            228983,
            228984,
            228985,
            228986,
            228987,
            228988,
            228989,
            228990
        };
    }
}
