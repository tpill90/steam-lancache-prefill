namespace SteamPrefill.Models.Enums
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    [Intellenum(typeof(string))]
    public sealed partial class Language
    {
        public static readonly Language English = new("english");

        public static readonly Language Arabic = new("arabic");
        public static readonly Language Brazilian = new("brazilian");
        public static readonly Language Bulgarian = new("bulgarian");

        public static readonly Language Chinese = new("chinese");
        public static readonly Language ChineseSimplified = new("schinese");
        public static readonly Language ChineseTraditional = new("tchinese");
        public static readonly Language Czech = new("czech");

        public static readonly Language Danish = new("danish");
        public static readonly Language Dutch = new("dutch");

        public static readonly Language Finnish = new("finnish");
        public static readonly Language French = new("french");
        public static readonly Language German = new("german");
        public static readonly Language Greek = new("greek");

        public static readonly Language Hungarian = new("hungarian");
        public static readonly Language Italian = new("italian");
        public static readonly Language Japanese = new("japanese");
        public static readonly Language Korean = new("koreana");
        public static readonly Language Latam = new("latam");

        public static readonly Language Norwegian = new("norwegian");
        public static readonly Language Polish = new("polish");
        public static readonly Language Portuguese = new("portuguese");
        public static readonly Language Spanish = new("spanish");

        public static readonly Language Romanian = new("romanian");
        public static readonly Language Russian = new("russian");
        public static readonly Language Thai = new("thai");
        public static readonly Language Turkish = new("turkish");
        public static readonly Language Swedish = new("swedish");
        public static readonly Language Ukrainian = new("ukrainian");
        public static readonly Language Vietnamese = new("vietnamese");
    }
}
