using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using Spectre.Console;
using SteamPrefill.Models;
using SteamPrefill.Utils;
using static SteamPrefill.Utils.SpectreColors;

// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("prefill", Description = "Downloads the latest version of one or more specified app(s)." +
                                           "  Automatically includes apps selected using the 'select-apps' command")]
    public class PrefillCommand : ICommand
    {

#if DEBUG // Experimental, debugging only
        [CommandOption("app")]
        public IReadOnlyList<uint> AppIds { get; init; }
#endif

        [CommandOption("all", Description = "Prefills all currently owned apps", Converter = typeof(NullableBoolConverter))]
        public bool? DownloadAllOwnedGames { get; init; }

        [CommandOption("force", 'f', 
            Description = "Forces the prefill to always run, overrides the default behavior of only prefilling if a newer version is available.", 
            Converter = typeof(NullableBoolConverter))]
        public bool? Force { get; init; }

        [CommandOption("dns-override", 'd', 
            Description = "Manually specifies the Lancache IP, used to prefill on the Lancache server.  Ex, '192.168.1.111'", 
            Converter = typeof(IpAddressConverter))]
        public IPAddress OverrideLancacheIp { get; init; }

        private IAnsiConsole _ansiConsole;
        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();

            await UpdateChecker.CheckForUpdatesAsync();

            var downloadArgs = new DownloadArguments
            {
                Force = Force ?? default(bool),
                OverrideLancacheIp = OverrideLancacheIp
            };

            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateSelectedAppIds(steamManager);

            try
            {
                steamManager.Initialize();
                List<uint> appIdsToDownload = BuildDownloadAppIdsList(steamManager);
                
                await steamManager.DownloadMultipleAppsAsync(appIdsToDownload);
            }
            catch (Exception e)
            {
                _ansiConsole.WriteException(e);
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

        private void ValidateSelectedAppIds(SteamManager steamManager)
        {
            var userSelectedApps = steamManager.LoadPreviouslySelectedApps();

#if DEBUG
            if (AppIds != null && AppIds.Any())
            {
                return;
            }
#endif

            if ((DownloadAllOwnedGames ?? default(bool)) || userSelectedApps.Any())
            {
                return;
            }
            _ansiConsole.MarkupLine(Red("No apps have been selected for prefill! At least 1 app is required!"));
            _ansiConsole.MarkupLine(Red($"Use the {Cyan("select-apps")} command to interactively choose which apps to prefill. "));
            _ansiConsole.MarkupLine("");
            _ansiConsole.Markup(Red($"Alternatively, the flag {LightYellow("--all")} can be specified to prefill all owned apps"));
            throw new CommandException(".", 1, true);
        }

        //TODO document
        //TODO can probably move this into steam manager itself
        private List<uint> BuildDownloadAppIdsList(SteamManager steamManager)
        {
            var appIdsToDownload = steamManager.LoadPreviouslySelectedApps();
            if (DownloadAllOwnedGames ?? default(bool))
            {
                appIdsToDownload.AddRange(steamManager.AllUserAppIds);
            }

#if DEBUG // Experimental, debugging only
            if (AppIds != null)
            {
                appIdsToDownload.AddRange(AppIds);
            }
#endif

            return appIdsToDownload;
        }
    }
}
