using System.Security.Cryptography;

namespace DepotDownloader.Utils
{
    public static class Util
    {
        public static byte[] ToSha1(this byte[] input)
        {
            using var sha = SHA1.Create();
            return sha.ComputeHash(input);
        }
    }
}
