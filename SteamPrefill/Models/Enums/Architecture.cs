namespace SteamPrefill.Models.Enums
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    [Intellenum(typeof(string))]
    public sealed partial class Architecture
    {
        public static readonly Architecture unknown = new("unknown");
        public static readonly Architecture x86 = new("32");
        public static readonly Architecture x64 = new("64");
    }
}
