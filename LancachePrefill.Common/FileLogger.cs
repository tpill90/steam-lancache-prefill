namespace LancachePrefill.Common
{
    /// <summary>
    /// A simple file logger, written with the intention of avoiding the complexity of adding Microsoft.Logging to this project,
    /// as well as avoiding the complexity of setting up Dependency Injection when it isn't needed in this project.
    /// </summary>
    public static class FileLogger
    {
        private const string LogFilePath = "app.log";
        private static readonly object LockObject = new object();

        public static void Log(string message)
        {
            // Avoids writing to the same file concurrently.
            lock (LockObject)
            {
                var messageNoAnsi = message.RemoveMarkup();
                File.AppendAllText(LogFilePath, $"[{DateTime.Now.ToString("h:mm:ss tt")}] {messageNoAnsi}\n");
            }
        }

        public static void LogException(string message, Exception e)
        {
            Log(message);
            Log(e.ToString());
        }
    }
}
