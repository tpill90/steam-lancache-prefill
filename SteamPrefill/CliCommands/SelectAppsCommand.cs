using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Spectre.Console;
using SteamPrefill.Utils;

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
            try
            {
                using var steamManager = new SteamManager(ansiConsole);
                steamManager.Initialize();

                await steamManager.SelectAppsAsync();
            }
            catch (Exception e)
            {
                ansiConsole.WriteException(e);
            }

            // For whatever reason, SteamKit2 prevents the application from closing normally.  Have to put this hack here so the app actually closes.
            Environment.Exit(0);
        }
    }
}
