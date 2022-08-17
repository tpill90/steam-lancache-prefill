namespace SteamPrefill.Utils
{
    public static class MiscExtensions
    {
        public static ConcurrentQueue<T> ToConcurrentBag<T>(this IOrderedEnumerable<T> list)
        {
            return new ConcurrentQueue<T>(list);
        }

        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is required by Steam")]
        public static byte[] ToSha1(this byte[] input)
        {
            using var sha = SHA1.Create();
            return sha.ComputeHash(input);
        }

        public static string FormatElapsedString(this Stopwatch stopwatch)
        {
            var elapsed = stopwatch.Elapsed;
            if (elapsed.TotalHours > 1)
            {
                return elapsed.ToString(@"h\:mm\:ss\.FF");
            }
            if (elapsed.TotalMinutes > 1)
            {
                return elapsed.ToString(@"mm\:ss\.FF");
            }
            return elapsed.ToString(@"ss\.FF");
        }
    }
}