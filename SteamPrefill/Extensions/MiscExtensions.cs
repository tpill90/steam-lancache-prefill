namespace SteamPrefill.Utils
{
    public static class MiscExtensions
    {
        public static bool Empty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
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
                return elapsed.ToString(@"h\:mm\:ss\.ff");
            }
            if (elapsed.TotalMinutes > 1)
            {
                return elapsed.ToString(@"mm\:ss\.ff");
            }
            return elapsed.ToString(@"ss\.ffff");
        }
    }

    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Substring(0, Math.Min(value.Length, maxLength));
        }

        /// <summary>
        /// Pads string with whitespace, taking the width of Unicode characters (2 wide) into account
        /// </summary>
        public static string PadRightUnicode(this string value, int totalWidth)
        {
            var unicodeWidth = value.Sum(t => UnicodeCalculator.GetWidth(t));

            // Adjusts the total padding by the additional width of the unicode characters
            return value.PadRight(totalWidth - (unicodeWidth - value.Length));
        }
    }
}