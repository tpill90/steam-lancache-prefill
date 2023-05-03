// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("select-apps", Description = "Displays an interactive list of all owned apps.  " +
                                          "As many apps as desired can be selected, which will then be used by the 'prefill' command")]
    public class SelectAppsCommand : ICommand
    {
        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoAnsiEscapeSequences { get; init; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            var ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            using var steamManager = new SteamManager(ansiConsole, new DownloadArguments());
            try
            {
                await steamManager.InitializeAsync();
                var tuiAppModels = await BuildTuiAppModelsAsync(steamManager);

                Application.UseSystemConsole = true;
                Application.Init();
                using var tui2 = new SelectAppsTui(tuiAppModels);
                Key userKeyPress = tui2.Run();

                // Will only allow for prefill if the user has saved changes.  Escape simply exists
                if (userKeyPress != Key.Enter)
                {
                    return;
                }
                steamManager.SetAppsAsSelected(tuiAppModels);

                // This escape sequence is required when running on linux, otherwise will not be able to use the Spectre selection prompt
                // See : https://github.com/gui-cs/Terminal.Gui/issues/418
                await console.Output.WriteAsync("\x1b[?1h");
                await console.Output.FlushAsync();

                var runPrefill = ansiConsole.Prompt(new SelectionPrompt<bool>()
                                                    .Title(LightYellow("Run prefill now?"))
                                                    .AddChoices(true, false)
                                                    .UseConverter(e => e == false ? "No" : "Yes"));

                if (runPrefill)
                {
                    await steamManager.DownloadMultipleAppsAsync(false, false, null);
                }
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

        private static async Task<List<TuiAppInfo>> BuildTuiAppModelsAsync(SteamManager steamManager)
        {
            // Listing user's owned apps, and selected apps
            var ownedApps = await steamManager.GetAllAvailableAppsAsync();
            var previouslySelectedApps = steamManager.LoadPreviouslySelectedApps();

            // Building out Tui models
            var tuiAppModels = ownedApps.Select(e =>
                                        new TuiAppInfo(e.AppId.ToString(), e.Name)
                                        {
                                            MinutesPlayed = e.MinutesPlayed2Weeks,
                                            ReleaseDate = e.ReleaseDate
                                        }).ToList();

            // Flagging previously selected apps as selected
            foreach (var appInfo in tuiAppModels)
            {
                appInfo.IsSelected = previouslySelectedApps.Contains(UInt32.Parse(appInfo.AppId));
            }

            return tuiAppModels;
        }
    }
}