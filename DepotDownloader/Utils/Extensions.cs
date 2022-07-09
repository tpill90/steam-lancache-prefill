using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ByteSizeLib;
using CliFx.Infrastructure;
using Spectre.Console;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader.Utils
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

        public static Status CreateSpectreStatusSpinner(this IAnsiConsole ansiConsole)
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
                                                     Prefix = FileSizePrefix.Decimal,
                                                     DisplayBits = true
                                                 });
            return spectreProgress;
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
        public static string ToDecimalString(this ByteSize byteSize)
        {
            return byteSize.ToString("0.##", CultureInfo.CurrentCulture, true);
        }
    }

    public static class Extensions
    {
        public static ConcurrentBag<T> ToConcurrentBag<T>(this IOrderedEnumerable<T> list)
        {
            return new ConcurrentBag<T>(list);
        }
    }
}