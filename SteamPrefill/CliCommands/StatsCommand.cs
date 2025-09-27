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

                //await steamManager.WriteStatsAsync(230411, 230410, @"C:\Users\Tim\Dropbox\Programming\Lancache-Prefills\Warframe Manifests.txt");
                await steamManager.WriteStatsAsync(526871, 526870, @"C:\Users\Tim\Dropbox\Programming\Lancache-Prefills\Satisfactory Manifests.txt");
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

    }
}
