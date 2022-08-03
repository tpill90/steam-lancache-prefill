namespace SteamPrefill.Models.Enums
{
    public enum ExcludedAppId
    {
        //TODO add Sam and max 104 to this list https://steamdb.info/app/8230/info/
        // Spacewar is a non-playable game, that is required for Steamworks multiplayer functionality to work
        SpaceWar = 480,
        PCGamerOnline = 92500,
        RisingStormBetaDedicatedServer = 238690,
        MinervaMetastasis = 235780,
        GamepadControllerTemplate = 1456390,
    }
}
