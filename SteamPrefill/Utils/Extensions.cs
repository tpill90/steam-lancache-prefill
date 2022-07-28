using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using ByteSizeLib;
using CliFx.Infrastructure;
using Spectre.Console;
using SteamKit2;
using SteamPrefill.Models.Enums;
using static SteamPrefill.Utils.SpectreColors;

namespace SteamPrefill.Utils
{
    public static class AnsiConsoleExtensions
    {
        public static IAnsiConsole CreateAnsiConsole(this IConsole console)
        {
            return AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Detect,
                ColorSystem = ColorSystemSupport.Detect,
                Out = new AnsiConsoleOutput(console.Output)
            });
        }

        public static Status StatusSpinner(this IAnsiConsole ansiConsole)
        {
            return ansiConsole.Status()
                              .AutoRefresh(true)
                              .SpinnerStyle(Style.Parse("green"))
                              .Spinner(Spinner.Known.Dots2);
        }

        public static Progress CreateSpectreProgress(this IAnsiConsole ansiConsole)
        {
            var spectreProgress = ansiConsole.Progress()
                                             .HideCompleted(true)
                                             .AutoClear(true)
                                             .Columns(
                                                 new TaskDescriptionColumn(),
                                                 new ProgressBarColumn(), 
                                                 new PercentageColumn(), 
                                                 new RemainingTimeColumn(), 
                                                 new DownloadedColumn(), 
                                                 new TransferSpeedColumn
                                                 {
                                                     Base = FileSizeBase.Decimal,
                                                     DisplayBits = true
                                                 });
            return spectreProgress;
        }

        public static string ReadPassword(this IAnsiConsole console, string promptText = null)
        {
            var defaultPrompt = $"Please enter your {Cyan("Steam password")}. {LightYellow("(Password won't be saved)")} : ";
            return console.Prompt(new TextPrompt<string>(promptText ?? defaultPrompt)
                                  .PromptStyle("white")
                                  .Secret());
        }

        public static void LogMarkup(this IAnsiConsole console, string message)
        {
            console.Markup($"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}");
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message)
        {
            console.MarkupLine($"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}");
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message, TimeSpan elapsed)
        {
            console.MarkupLine($"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}".PadRight(65) + Yellow(elapsed.ToString(@"ss\.FFFF")));
        }
    }

    public static class ByteSizeExtensions
    {
        public static string ToDecimalString(this ref ByteSize byteSize)
        {
            return byteSize.ToString("0.##", CultureInfo.CurrentCulture, true);
        }
    }

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

        public static string FormattedElapsedString(this Stopwatch stopwatch)
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

    public static class KeyValueExtensions
    {
        /// <summary>
        /// Attempts to convert and return the value of this instance as an unsigned long.
        /// If the conversion is invalid, null is returned.
        /// </summary>
        /// <returns>The value of this instance as an unsigned long.</returns>
        public static ulong? AsUnsignedLongNullable(this KeyValue keyValue)
        {
            ulong value;

            if (ulong.TryParse(keyValue.Value, out value) == false)
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an unsigned int.
        /// If the conversion is invalid, null is returned.
        /// </summary>
        /// <returns>The value of this instance as an unsigned int.</returns>
        public static uint? AsUnsignedIntNullable(this KeyValue keyValue)
        {
            uint value;

            if (uint.TryParse(keyValue.Value, out value) == false)
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// Attempts to convert and return the value of this instance as an enum.
        /// If the conversion is invalid, null is returned.
        /// </summary>
        public static T AsEnum<T>(this KeyValue keyValue, bool toLower = false) where T : EnumBase<T>
        {
            if (keyValue == KeyValue.Invalid)
            {
                return null;
            }
            if (String.IsNullOrEmpty(keyValue.Value))
            {
                return null;
            }
            if (toLower)
            {
                return EnumBase<T>.Parse(keyValue.Value.ToLower());
            }
            return EnumBase<T>.Parse(keyValue.Value);
        }

        public static List<string> SplitCommaDelimited(this KeyValue keyValue)
        {
            if (keyValue == KeyValue.Invalid || String.IsNullOrEmpty(keyValue.Value))
            {
                return new List<string>();
            }
            return keyValue.Value.Split(",").ToList();
        }
    }
}