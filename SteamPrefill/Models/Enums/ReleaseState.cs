namespace SteamPrefill.Models.Enums
{
    public class ReleaseState : EnumBase<ReleaseState>
    {
        public static readonly ReleaseState eStateAvailable = new ReleaseState("eStateAvailable");
        public static readonly ReleaseState eStateAvailablePreloadable = new ReleaseState("eStateAvailablePreloadable");
        public static readonly ReleaseState eStateAvailablea = new ReleaseState("eStateAvailablea");

        public static readonly ReleaseState eStateComingAvailable = new ReleaseState("eStateComingAvailable");

        public static readonly ReleaseState eStateComingSoonNoPreload = new ReleaseState("eStateComingSoonNoPreload");
        public static readonly ReleaseState eStateJustReleased = new ReleaseState("eStateJustReleased");

        public static readonly ReleaseState eStatePreloadOnly = new ReleaseState("eStatePreloadOnly");

        public static readonly ReleaseState eStateTool = new ReleaseState("eStateTool");
        public static readonly ReleaseState eStateUnAvailable = new ReleaseState("eStateUnAvailable");

        private ReleaseState(string name) : base(name)
        {
        }
    }
}
