namespace SteamPrefill.Models.Enums
{
    [Intellenum(typeof(string))]
    public sealed partial class SortOrder
    {
        public static readonly SortOrder Ascending = new("ascending");
        public static readonly SortOrder Descending = new("descending");
    }

    [Intellenum(typeof(string))]
    public sealed partial class SortColumn
    {
        public static readonly SortColumn App = new("app");
        public static readonly SortColumn Size = new("size");
    }
}