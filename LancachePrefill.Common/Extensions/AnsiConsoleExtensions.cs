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
            var defaultPrompt = $"Please enter your {SpectreColors.Cyan("Steam password")}. {SpectreColors.LightYellow("(Password won't be saved)")} : ";
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

        public static void LogMarkupLine(this IAnsiConsole console, string message, Stopwatch stopwatch)
        {
            console.LogMarkupLine(message, stopwatch.Elapsed);
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message, TimeSpan elapsed)
        {
            console.MarkupLine($"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}".PadRight(65) + SpectreColors.LightYellow(elapsed.ToString(@"ss\.FFFF")));
        }
    }
}