using System;
using System.Security.Cryptography;
using System.Text;
using Spectre.Console;

namespace DepotDownloader.Utils
{
    static class Util
    {
        public static string GetSteamArch()
        {
            return Environment.Is64BitOperatingSystem ? "64" : "32";
        }

        //TODO this isnt secure at all
        public static string ReadPassword()
        {
            //TODO need a far better user interface for this
            AnsiConsole.WriteLine("Please enter password");
            ConsoleKeyInfo keyInfo;
            var password = new StringBuilder();

            do
            {
                keyInfo = Console.ReadKey(true);

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password.Remove(password.Length - 1, 1);
                        Console.Write("\b \b");
                    }

                    continue;
                }

                /* Printable ASCII characters only */
                var c = keyInfo.KeyChar;
                if (c >= ' ' && c <= '~')
                {
                    password.Append(c);
                    Console.Write('*');
                }
            } while (keyInfo.Key != ConsoleKey.Enter);

            return password.ToString();
        }

        public static byte[] ToShaHash(this byte[] input)
        {
            using var sha = SHA1.Create();
            return sha.ComputeHash(input);
        }
        
    }
}
