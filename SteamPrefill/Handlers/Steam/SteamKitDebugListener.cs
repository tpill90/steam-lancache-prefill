namespace SteamPrefill.Handlers.Steam
{
    public class SteamKitDebugListener : IDebugListener
    {
        private readonly IAnsiConsole _ansiConsole;

        public SteamKitDebugListener(IAnsiConsole ansiConsole)
        {
            if (ansiConsole == null)
            {
                throw new ArgumentException("ansiConsole cannot be null");
            }
            _ansiConsole = ansiConsole;
        }

        public void WriteLine(string category, string msg)
        {
            _ansiConsole.MarkupLine($"SteamKitDebug - {category}: {msg}");
        }
    }
}