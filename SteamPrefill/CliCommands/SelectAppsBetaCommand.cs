using Terminal.Gui;


// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    //TODO update description
    [UsedImplicitly]
    [Command("select-apps-beta", Description = "Displays an interactive list of all owned apps.  " +
                                          "As many apps as desired can be selected, which will then be used by the 'prefill' command")]
    public class SelectAppsBetaCommand : ICommand
    {
        //TODO update docs explaining that there is a beta
        public async ValueTask ExecuteAsync(IConsole console)
        {
            var ansiConsole = console.CreateAnsiConsole();
            using var steamManager = new SteamManager(ansiConsole, new DownloadArguments());

            try
            {
                steamManager.Initialize();

                var games = await steamManager.GetGames();
                steamManager.Shutdown();

                Application.Init();
                var tui2 = new SelectAppsTui(games);
                tui2.Run();
                

                //TODO
                //var runPrefill = ansiConsole.Prompt(new SelectionPrompt<bool>()
                //                    .Title(SpectreColors.LightYellow("Run prefill now?"))
                //                    .AddChoices(true, false)
                //                    .UseConverter(e => e == false ? "No" : "Yes"));
                //if (runPrefill)
                //{
                //    await steamManager.DownloadMultipleAppsAsync(false, new List<uint>());
                //}
            }
            catch (Exception e)
            {
                ansiConsole.WriteException(e, ExceptionFormats.ShortenPaths);
            }
        }
    }
}
