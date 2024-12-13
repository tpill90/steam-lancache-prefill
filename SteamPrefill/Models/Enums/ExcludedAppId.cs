namespace SteamPrefill.Models.Enums
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public enum ExcludedAppId
    {
        /// <summary>
        /// SpaceWar is a non-playable game, that is required for Steamworks multiplayer functionality to work
        ///
        /// https://www.rockpapershotgun.com/spacewar-why-a-hidden-ancient-game-keeps-showing-in-steams-most-played-games
        /// </summary>
        SpaceWar = 480
    }
}
