namespace SteamPrefill.Models.Enums
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public enum ExcludedAppId
    {
        // Spacewar is a non-playable game, that is required for Steamworks multiplayer functionality to work
        SpaceWar = 480,
        PCGamerOnline = 92500,
        RisingStormBetaDedicatedServer = 238690,
        MinervaMetastasis = 235780,
        GamepadControllerTemplate = 1456390,
    }
}
