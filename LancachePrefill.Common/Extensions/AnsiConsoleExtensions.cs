namespace LancachePrefill.Common.Extensions
{
    public static class AnsiConsoleExtensions
    {
        /// <summary>
        /// When enabled, log messages written using <see cref="LogMarkupVerbose"/> will be printed to the console,
        /// in addition to being logged to the log file.
        /// </summary>
        public static bool WriteVerboseLogs { get; set; }

        /// <summary>
        /// Writes text formatted with ANSI escape sequences console, without a newline.
        /// </summary>
        public static void LogMarkup(this IAnsiConsole console, string message)
        {
            console.Markup($"{FormattedTime} {message}");
        }

        public static void LogMarkupLine(this IAnsiConsole console, string message)
        {
            console.MarkupLine($"{FormattedTime} {message}");
            FileLogger.Log(message);
        }

        /// <summary>
        /// Writes text formatted with ANSI escape sequences console, with the elapsed time appended.
        /// </summary>
        /// <param name="message">Message text formatted with ANSI escape sequences</param>
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

        /// <summary>
        /// Will log error messages to the console, only when <see cref="WriteVerboseLogs"/> has been set to true.
        /// </summary>
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

        /// <summary>
        /// Logs an error message to the console, as well as to the log file.
        /// </summary>
        public static void LogMarkupError(this IAnsiConsole console, string message)
        {
            console.MarkupLine($"{FormattedTime} {Red(message)}");
            FileLogger.Log(message);
        }

        private static string FormattedTime => $"[[{DateTime.Now.ToString("h:mm:ss tt")}]]";
    }
}