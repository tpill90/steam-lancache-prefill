namespace LancachePrefill.Common
{
    public static class FileLogger
    {
        private static string _logFilePath = "errorlog.txt";

        //TODO need to move this over to using a logging library, rather than doing this manually
        public static void Log(string message)
        {
            File.AppendAllText(_logFilePath, $"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}");
        }
    }
}
