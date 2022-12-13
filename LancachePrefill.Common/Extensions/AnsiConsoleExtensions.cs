namespace LancachePrefill.Common.Extensions
{
    //TODO comment
    public static class AnsiConsoleExtensions
    {
        //TODO I don't particularly like this.  Refactor into something more sane
        //TODO comment
        public static bool WriteVerboseLogs = false;

        private static string FormattedTime => $"[[{DateTime.Now.ToString("h:mm:ss tt")}]]";

        /// <summary>
        /// Writes formatted markup text to the console, without a newline.
        /// </summary>
        /// <param name="console"></param>
        /// <param name="message"></param>
        public static void LogMarkup(this IAnsiConsole console, string message)
        {
            console.Markup($"{FormattedTime} {message}");
        }

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

        //TODO Replace LogMarkupLine() instances in the codebase that could use this instead.
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
    }
}