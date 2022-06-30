using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using DepotDownloader.Protos;
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
            //TODO document
            //TODO required
            [CommandOption("app")]
            public IReadOnlyList<uint> AppIds { get; init; }

            //TODO document
            //TODO required
            [CommandOption("username")]
            public string Username { get; init; }

            //TODO document
            [CommandOption("remember-password")]
            public bool RememberPassword { get; init; }

            public async ValueTask ExecuteAsync(IConsole console)
            {
                try
                {
                    var timer = Stopwatch.StartNew();
                    var ansiConsole = console.CreateAnsiConsole();

                    AccountSettingsStore.LoadFromFile("account.config");
                    //TODO remove;
                    var password = File.ReadAllText(@"C:\Users\Tim\Desktop\password.txt");

                    var steamManager = new SteamManager(ansiConsole);
                    await steamManager.Initialize(Username, password, RememberPassword);

                    var distinctAppIds = AppIds.Distinct().ToList();
                    await steamManager.BulkLoadAppInfos(distinctAppIds);

                    foreach (var app in distinctAppIds)
                    {
                        // TODO need to implement the rest of the cli parameters
                        var downloadArgs = new DownloadArguments
                        {
                            Username = Username,
                            AppId = app,
                        };
                        await steamManager.DownloadAppAsync(downloadArgs);
                    }

                    //TODO prefill needs to include hours + minutes
                    ansiConsole.LogMarkupLine($"Completed prefill in {Yellow(timer.Elapsed.ToString(@"ss\.FFFF"))}");
                }
                catch (Exception e)
                {
                    //TODO handle
                    AnsiConsole.WriteException(e);
                }
                // TODO this feels like a hack, but for whatever reason the application hangs if you don't explicitly call the logout method
                Environment.Exit(0);
            }
        }
    }
}