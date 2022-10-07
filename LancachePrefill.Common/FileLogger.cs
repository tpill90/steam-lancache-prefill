namespace LancachePrefill.Common
{
    public static class FileLogger
    {
        private static string _logFilePath = "log.txt";

        //TODO need to move this over to using a logging library, rather than doing this manually
        public static void Log(string message)
        {
            File.AppendAllText(_logFilePath, $"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}");
        }

        public static void Log(LogLevel level, string message)
        {
            Log(string.Format("{0} {1}", level.ToString(), message));
        }

        public enum LogLevel { INFO, DEBUG, ERROR, FATAL }
    }
}
