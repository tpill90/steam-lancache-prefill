using Spectre.Console;
using SteamKit2;

namespace SteamPrefill.Utils
{
    /// <summary>
    /// Enable with :
    /// DebugLog.AddListener(new SteamKitDebugListener(_ansiConsole));
    /// DebugLog.Enabled = true;
    /// </summary>
    public class SteamKitDebugListener : IDebugListener
    {
        private readonly IAnsiConsole _ansiConsole;

        public SteamKitDebugListener(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
        }

        public void WriteLine(string category, string msg)
        {
            _ansiConsole.MarkupLine($"SteamKitDebug - {category}: {msg}");
        }
    }
}