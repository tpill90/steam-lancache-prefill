using static LancachePrefill.Common.Extensions.TransferSpeedUnit;
using Spectre.Console;

namespace LancachePrefill.Common.Extensions
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

        public static Progress CreateSpectreProgress(this IAnsiConsole ansiConsole, TransferSpeedUnit unit)
        {
            var displayBits = unit == Bits;
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
                                                     DisplayBits = displayBits
                                                 });
            return spectreProgress;
        }

        public static string ReadPassword(this IAnsiConsole console, string promptText = null)
        {
            var promptTask = Task.Run(() =>
            {
                var defaultPrompt = $"Please enter your {Cyan("Steam password")}. {LightYellow("(Password won't be saved)")} : ";
                return console.Prompt(new TextPrompt<string>(promptText ?? defaultPrompt)
                                      .PromptStyle("white")
                                      .Secret());
            });
            return promptTask.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }

        public static void LogMarkup(this IAnsiConsole console, string message)
        {
            console.Markup($"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}");
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message)
        {
            console.MarkupLine($"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}");
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message, Stopwatch stopwatch)
        {
            console.LogMarkupLine(message, stopwatch.Elapsed);
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message, TimeSpan elapsed)
        {
            console.MarkupLine($"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}".PadRight(65) + SpectreColors.LightYellow(elapsed.ToString(@"ss\.FFFF")));
        }
    }

    public class TransferSpeedUnit : EnumBase<TransferSpeedUnit>
    {
        public static readonly TransferSpeedUnit Bits = new TransferSpeedUnit("bits");
        public static readonly TransferSpeedUnit Bytes = new TransferSpeedUnit("bytes");

        private TransferSpeedUnit(string name) : base(name)
        {
        }
    }
}