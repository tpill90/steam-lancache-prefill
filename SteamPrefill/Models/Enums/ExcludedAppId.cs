namespace SteamPrefill.Models.Enums
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public enum ExcludedAppId
    {
        CodenameGordon = 92,

        /// <summary>
        /// SpaceWar is a non-playable game, that is required for Steamworks multiplayer functionality to work
        ///
        /// https://www.rockpapershotgun.com/spacewar-why-a-hidden-ancient-game-keeps-showing-in-steams-most-played-games
        /// </summary>
        SpaceWar = 480,
        PCGamerOnline = 92500,
        RisingStormBetaDedicatedServer = 238690,
        MinervaMetastasis = 235780,
        GamepadControllerTemplate = 1456390
    }
}
