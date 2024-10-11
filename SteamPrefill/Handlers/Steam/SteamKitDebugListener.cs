namespace SteamPrefill.Handlers.Steam
{
    public sealed class SteamKitDebugListener : IDebugListener
    {
        private readonly IAnsiConsole _ansiConsole;

        public SteamKitDebugListener(IAnsiConsole ansiConsole)
        {
            DebugLog.Enabled = true;

            if (ansiConsole == null)
            {
                throw new ArgumentException("ansiConsole cannot be null");
            }
            _ansiConsole = ansiConsole;
        }

        public void WriteLine(string category, string msg)
        {
            // Removing GUID that just pollutes the output
            category = Regex.Replace(category, @"^[a-fA-F0-9]{32}/", string.Empty);

            // Colorizing the CM url + removing the annoying unspecified prefix
            msg = Regex.Replace(msg, @"(?:[a-zA-Z0-9\-]+)\.steamserver\.net", match => Cyan(match.Value));
            msg = msg.Replace("Unspecified/", "");

            _ansiConsole.LogMarkupLine($"{LightGreen(category)}: {msg}");
        }
    }
}