namespace LancachePrefill.Common
{
    public static class FileLogger
    {
        private static string _logFilePath = "app.log";

        //TODO need to move this over to using a logging library, rather than doing this manually
        public static void Log(string message)
        {
            var messageNoAnsi = message.RemoveMarkup();
            File.AppendAllText(_logFilePath, $"[{DateTime.Now.ToString("h:mm:ss tt")}] {messageNoAnsi}\n");
        }

        public static void LogException(string message, Exception e)
        {
            Log(message);
            Log(e.ToString());
        }
    }
}
