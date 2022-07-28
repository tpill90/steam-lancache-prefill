namespace SteamPrefill.Models.Enums
{
    public class Architecture : EnumBase<Architecture>
    {
        public static readonly Architecture x86 = new Architecture("32");
        public static readonly Architecture x64 = new Architecture("64");

        private Architecture(string name) : base(name)
        {
        }
    }
}
