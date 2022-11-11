namespace SteamPrefill.Utils
{
    public static class MiscExtensions
    {
        public static bool Empty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }

        public static ConcurrentStack<T> ToConcurrentStack<T>(this IEnumerable<T> list)
        {
            return new ConcurrentStack<T>(list);
        }

        public static void AddRange<T>(this HashSet<T> hashSet, List<T> values)
        {
            foreach (var value in values)
            {
                hashSet.Add(value);
            }
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

        [SuppressMessage("Security", "CA5394:Random is an insecure RNG", Justification = "Security doesn't matter here, just need to shuffle requests.")]
        public static void Shuffle<T>(this IList<T> list)
        {
            var random = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
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