// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.

namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("select-apps-beta", Description = "Beta version of select-apps interface redesign")]
    public class SelectAppsBetaCommand : ICommand
    {
        //TODO remove on 11/01/2022
        public async ValueTask ExecuteAsync(IConsole console)
        {
            var ansiConsole = console.CreateAnsiConsole();
            var table = new Table
            {
                ShowHeaders = false,
                Border = TableBorder.Rounded,
                BorderStyle = new Style(Spectre.Console.Color.Yellow4)
            };
            table.AddColumn("");

            // Add some rows
            table.AddRow("");
            table.AddRow("These beta changes have now been officially released!");
            table.AddRow($"{LightYellow("select-apps")} should now be used instead!");
            table.AddRow("");

            // Render the table to the console
            ansiConsole.Write(table);

            // Required so that this method is async
            await Task.CompletedTask;
        }
    }
}