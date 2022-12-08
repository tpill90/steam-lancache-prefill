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

        public static Progress CreateSpectreProgress(this IAnsiConsole ansiConsole, TransferSpeedUnit unit, bool displayTransferRate = true)
        {
            var displayBits = unit == TransferSpeedUnit.Bits;
            var columns = new List<ProgressColumn>
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn()
            };
            if (displayTransferRate)
            {
                columns.Add(new DownloadedColumn());
                columns.Add(new TransferSpeedColumn { Base = FileSizeBase.Decimal, DisplayBits = displayBits });
            }
            var spectreProgress = ansiConsole.Progress()
                                             .HideCompleted(true)
                                             .AutoClear(true)
                                             .Columns(columns.ToArray());

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

        //TODO I don't particularly like this.  Refactor into something more sane
        //TODO comment
        public static bool WriteVerboseLogs = false;

        //TODO break these timer methods out into their own extension class
        //TODO comment
        private static string FormattedTime => $"[[{DateTime.Now.ToString("h:mm:ss")} {DateTime.Now.ToString("tt")}]]";

        //TODO comment
        public static void LogMarkup(this IAnsiConsole console, string message)
        {
            console.Markup($"{FormattedTime} {message}");
        }

        //TODO comment
        public static void LogMarkupLine(this IAnsiConsole console, string message)
        {
            console.MarkupLine($"{FormattedTime} {message}");
            FileLogger.Log(message);
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message, Stopwatch stopwatch)
        {
            var messageWithTime = $"{FormattedTime} {message}";
            // Taking the difference between the original message length, and the message length with markup removed.  
            // Ensures that PadRight will align messages with markup correctly.
            var paddingDiff = messageWithTime.Length - new Markup(messageWithTime).Length;

            var formattedElapsedTime = stopwatch.Elapsed.ToString(@"ss\.FFFF");
            console.MarkupLine(messageWithTime.PadRight(65 + paddingDiff) + LightYellow(formattedElapsedTime));
            FileLogger.Log($"{message} {formattedElapsedTime}");
        }

        //TODO comment
        //TODO Replace LogMarkupLine() instances in the codebase that could use this instead.
        public static void LogMarkupVerbose(this IAnsiConsole console, string message)
        {
            // Always write to the logfile
            FileLogger.Log(message);

            // Skip writing to console unless verbose logging is enabled
            if (!WriteVerboseLogs)
            {
                return;
            }
            console.MarkupLine($"{FormattedTime} {message}");
        }

        //TODO comment
        public static void LogMarkupError(this IAnsiConsole console, string message)
        {
            console.MarkupLine($"{FormattedTime} {Red(message)}");
            FileLogger.Log(message);
        }

        public static Markup ToMarkup(this Object obj)
        {
            return new Markup(obj.ToString());
        }
    }

    public sealed class TransferSpeedUnit : EnumBase<TransferSpeedUnit>
    {
        public static readonly TransferSpeedUnit Bits = new TransferSpeedUnit("bits");
        public static readonly TransferSpeedUnit Bytes = new TransferSpeedUnit("bytes");

        private TransferSpeedUnit(string name) : base(name)
        {
        }
    }
}