using AnsiConsoleExtensions = LancachePrefill.Common.Extensions.AnsiConsoleExtensions;
// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("select-apps", Description = "Displays an interactive list of all owned apps.  " +
                                          "As many apps as desired can be selected, which will then be used by the 'prefill' command")]
    public class SelectAppsCommand : ICommand
    {
        public async ValueTask ExecuteAsync(IConsole console)
        {
            var ansiConsole = console.CreateAnsiConsole();
            using var steamManager = new SteamManager(ansiConsole, new DownloadArguments());
            try
            {
                WriteBetaMessage(ansiConsole);

                steamManager.Initialize();
                await steamManager.SelectAppsAsync();

                var runPrefill = ansiConsole.Prompt(new SelectionPrompt<bool>()
                                                    .Title(LightYellow("Run prefill now?"))
                                                    .AddChoices(true, false)
                                                    .UseConverter(e => e == false ? "No" : "Yes"));

                uint? cellId = ansiConsole.Prompt(new SelectionPrompt<uint?>()
                    .Title(LightYellow("Select Download Region:"))
                    .AddChoices(new uint?[] { 1, 5, 9, null }) //Maybe random from https://github.com/tpill90/steam-lancache-prefill/issues/135#issuecomment-1264783642
                    .UseConverter(e => {
                        if (e == 9)
                        {
                            return "SEA";
                        }
                        else if (e == 5)
                        {
                            return "Europe";
                        }
                        else if (e == 1)
                        {
                            return "Americas";
                        }
                        else
                        {
                            return null;
                        }
                    }));

                if (cellId.HasValue)
                {
                    File.Delete(AppConfig.UserSelectedCellId);
                    await File.WriteAllTextAsync(AppConfig.UserSelectedCellId, cellId.ToString());
                }

                if (runPrefill)
                {
                    await steamManager.DownloadMultipleAppsAsync(false, false, null, new List<uint>());
                }
            }
            catch (TimeoutException e)
            {
                ansiConsole.MarkupLine("\n");
                if (e.StackTrace.Contains(nameof(UserAccountStore.GetUsername)))
                {
                    ansiConsole.MarkupLine(Red("Timed out while waiting for username entry"));
                }
                if (e.StackTrace.Contains(nameof(AnsiConsoleExtensions.ReadPassword)))
                {
                    ansiConsole.MarkupLine(Red("Timed out while waiting for password entry"));
                }
                ansiConsole.WriteException(e, ExceptionFormats.ShortenPaths);
            }
            catch (Exception e)
            {
                ansiConsole.WriteException(e, ExceptionFormats.ShortenPaths);
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

        private void WriteBetaMessage(IAnsiConsole ansiConsole)
        {
            var table = new Table
            {
                ShowHeaders = false,
                Border = TableBorder.Rounded,
                BorderStyle = new Style(Color.Yellow4)
            };
            table.AddColumn("");

            // Add some rows
            table.AddRow("");
            table.AddRow($"select-apps is getting a new look!");
            table.AddRow($"Try it out now with {LightYellow("select-apps-beta")}!");
            table.AddRow("");
            table.AddRow("Please direct beta feedback to :  ");
            table.AddRow(LightBlue("https://github.com/tpill90/steam-lancache-prefill/issues/60"));
            table.AddRow("");

            // Render the table to the console
            ansiConsole.Write(table);
        }
    }
}
