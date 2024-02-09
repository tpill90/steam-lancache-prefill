namespace SteamPrefill.Models.Enums
{
    [Intellenum(typeof(string))]
    public sealed partial class SortOrder
    {
        public static readonly SortOrder Ascending = new("ascending");
        public static readonly SortOrder Descending = new("descending");
    }
}