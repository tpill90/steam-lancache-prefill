namespace SteamPrefill.Models.Enums
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public class Language : EnumBase<Language>
    {
        public static readonly Language English = new Language("english");

        public static readonly Language Arabic = new Language("arabic");
        public static readonly Language Brazilian = new Language("brazilian");
        public static readonly Language Bulgarian = new Language("bulgarian");

        public static readonly Language Chinese = new Language("chinese");
        public static readonly Language ChineseSimplified = new Language("schinese");
        public static readonly Language ChineseTraditional = new Language("tchinese");
        public static readonly Language Czech = new Language("czech");

        public static readonly Language Danish = new Language("danish");
        public static readonly Language Dutch = new Language("dutch");

        public static readonly Language Finnish = new Language("finnish");
        public static readonly Language French = new Language("french");
        public static readonly Language German = new Language("german");
        public static readonly Language Greek = new Language("greek");

        public static readonly Language Hungarian = new Language("hungarian");
        public static readonly Language Italian = new Language("italian");
        public static readonly Language Japanese = new Language("japanese");
        public static readonly Language Korean = new Language("koreana");
        public static readonly Language Latam = new Language("latam");

        public static readonly Language Norwegian = new Language("norwegian");
        public static readonly Language Polish = new Language("polish");
        public static readonly Language Portuguese = new Language("portuguese");
        public static readonly Language Spanish = new Language("spanish");

        public static readonly Language Romanian = new Language("romanian");
        public static readonly Language Russian = new Language("russian");
        public static readonly Language Thai = new Language("thai");
        public static readonly Language Turkish = new Language("turkish");
        public static readonly Language Swedish = new Language("swedish");
        public static readonly Language Ukrainian = new Language("ukrainian");
        public static readonly Language Vietnamese = new Language("vietnamese");

        private Language(string name) : base(name)
        {
        }
    }
}
