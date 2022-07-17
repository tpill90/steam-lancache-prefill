using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using DepotDownloader.Utils;
using JetBrains.Annotations;
using Spectre.Console;
using static DepotDownloader.Utils.SpectreColors;

// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace DepotDownloader
{
    [UsedImplicitly]
    public sealed class CliCommands
    {
        //TODO document
        [UsedImplicitly]
        [Command("prefill", Description = "Downloads the latest files for one or more specified apps(s)")]
        public class PrefillCommand : ICommand
        {
            //TODO document + description
            //TODO required
            //TODO validate that these are valid app ids, and prompt the user if they arent
            [CommandOption("app")]
            public IReadOnlyList<uint> AppIds { get; init; }

            //TODO document + description
            //TODO required
            [CommandOption("username")]
            public string Username { get; init; }

            //TODO document + description
            [CommandOption("all")]
            public bool DownloadAllOwnedGames { get; init; }

            //TODO document + description
            [CommandOption(name: "force", shortName: 'f')]
            public bool Force { get; init; }

            public async ValueTask ExecuteAsync(IConsole console)
            {
                var timer = Stopwatch.StartNew();
                var ansiConsole = console.CreateAnsiConsole();
                try
                {
                    var steamManager = new SteamManager(ansiConsole);
                    steamManager.Initialize(Username);

                    // Determining which app ids to download
                    //TODO validate that there was at least 1 id passed in
                    var appIdsToDownload = new List<uint>();
                    if (DownloadAllOwnedGames)
                    {
                        appIdsToDownload.AddRange(steamManager.GetAllUserAppIds());
                    }
                    if (AppIds != null)
                    {
                        appIdsToDownload.AddRange(AppIds);
                    }

                    // Configure DownloadArgs
                    var downloadArgs = new DownloadArguments
                    {
                        Force = Force
                    };

                    await steamManager.DownloadMultipleAppsAsync(appIdsToDownload, downloadArgs);

                    //TODO prefill needs to include hours + minutes
                    ansiConsole.LogMarkupLine($"Completed prefill in {Yellow(timer.Elapsed.ToString(@"ss\.FFFF"))}");
                }
                catch (Exception e)
                {
                    //TODO handle
                    ansiConsole.WriteException(e);
                }
                // TODO this feels like a hack, but for whatever reason the application hangs if you don't explicitly call the logout method
                Environment.Exit(0);
            }
        }

        //TODO document
        //TODO add description
        [UsedImplicitly]
        [Command("select-apps")]
        public class SelectAppsCommand : ICommand
        {
            //TODO document + description
            //TODO required
            [CommandOption("username")]
            public string Username { get; init; }

            //TODO for some reason, when logging in for the first time, it doesn't save the steam session and reprompts you for password each time
            public async ValueTask ExecuteAsync(IConsole console)
            {
                var timer = Stopwatch.StartNew();
                var ansiConsole = console.CreateAnsiConsole();
                try
                {
                    var steamManager = new SteamManager(ansiConsole);
                    steamManager.Initialize(Username);

                    await steamManager.SelectApps();

                    
                    
                }
                catch (Exception e)
                {
                    //TODO handle
                    ansiConsole.WriteException(e);
                }
                // TODO this feels like a hack, but for whatever reason the application hangs if you don't explicitly call the logout method
                Environment.Exit(0);
            }
        }
    }
}