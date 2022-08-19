namespace LancachePrefill.Common
{
    public static class FileLogger
    {
        private static string _logFilePath = "errorlog.txt";

        public static void Log(string message)
        {
            File.AppendAllText(_logFilePath, $"[[{DateTime.Now.ToString("h:mm:ss tt")}]] {message}");
        }
    }
}
