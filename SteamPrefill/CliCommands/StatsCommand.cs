// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("stats", Description = "")]
    public class StatsCommand : ICommand
    {
        private IAnsiConsole _ansiConsole;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();

            using var steamManager = new SteamManager(_ansiConsole, new DownloadArguments());

            try
            {
                await steamManager.InitializeAsync();
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

    }
}
