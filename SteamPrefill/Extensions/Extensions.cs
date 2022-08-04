using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Security.Cryptography;

namespace SteamPrefill.Utils
{
    public static class Extensions
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

        /// <summary>
        /// An extension method to determine if an IP address is internal, as specified in RFC1918
        /// </summary>
        /// <param name="toTest">The IP address that will be tested</param>
        /// <returns>Returns true if the IP is internal, false if it is external</returns>
        public static bool IsInternal(this IPAddress toTest)
        {
            if (IPAddress.IsLoopback(toTest))
            {
                return true;
            }
            if (toTest.ToString() == "::1")
            {
                return false;
            }

            byte[] bytes = toTest.GetAddressBytes();
            switch (bytes[0])
            {
                case 10:
                    return true;
                case 172:
                    return bytes[1] < 32 && bytes[1] >= 16;
                case 192:
                    return bytes[1] == 168;
                default:
                    return false;
            }
        }
    }
}