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
                    await steamManager.Initialize(Username);

                    // Determining which app ids to download
                    //TODO validate that there was at least 1 id passed in
                    var appIdsToDownload = new List<uint>();
                    if (DownloadAllOwnedGames)
                    {
                        //TODO there has to be a better way to know all the owned games, without including the invalid ones. Might be able to use the steam web api to do this.
                        appIdsToDownload.AddRange(steamManager._steam3.OwnedAppIds);
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
    }
}