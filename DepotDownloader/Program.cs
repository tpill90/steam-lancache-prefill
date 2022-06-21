using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Protos;
using DepotDownloader.Utils;
using Spectre.Console;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader
{
    // TODO - General - Rename Solution/csproj/etc from DepotDownloader to SteamPrefill
    // TODO - General - Start implementing features from lancache-autofill https://github.com/zeropingheroes/lancache-autofill
    // TODO - General - Write check to see if a newer version is available, and display a message to the user if there is
    // TODO - General - Add publish script
    // TODO - General - Promote this app on r/lanparty and discord once it is finished.
    // TODO - General - Cleanup Console.WriteLine calls
    // TODO - Metrics - Setup and configure Github historical statistics (Downloads, page views, etc).  This will be useful for seeing project usage.
    // TODO - Documentation - Update documentation in readme
    // TODO - Documentation - Include reasons as to why this program is better than the one it replaces.  Download speed, no dependencies, no disk writes.
    // TODO - Documentation - Update documentation to show why you should be using UTF16.  Include an image showing before/after.  Also possibly do a check on startup?
    // TODO - Performance - Benchmark allocations
    // TODO - Performance - Finish reducing allocation warnings in DPA.
    // TODO - Performance - Program startup is really slow.  Seems mostly related to logging into Steam. Should log how long certain things take
    // TODO - Performance - Figure out why this app isn't consistently hitting 10gbit download speeds.
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Spectre - Cleanup all instances of AnsiConsole.Console

    // TODO - General - Cleanup CdnPool class
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var timer = Stopwatch.StartNew();
            //TODO remove this ansiconsole
            var ansiConsole = AnsiConsole.Console;

            AccountSettingsStore.LoadFromFile("account.config");
            DownloadArguments downloadArgs = ParseArguments(args);

            var steamManager = new SteamManager(ansiConsole);
            //TODO remove
            downloadArgs.Password = File.ReadAllText(@"C:\Users\Tim\Desktop\password.txt");


            await steamManager.Initialize(downloadArgs.Username, downloadArgs.Password);
            await steamManager.DownloadAppAsync(downloadArgs);
            steamManager.Shutdown();
           
            ansiConsole.LogMarkupLine($"Completed prefill in {Yellow(timer.Elapsed.ToString(@"ss\.FFFF"))}");

            // TODO this feels like a hack, but for whatever reason the application hangs if you don't explicitly call the logout method
            Environment.Exit(0);
            return 0;
        }

        public static DownloadArguments ParseArguments(string[] args)
        {
            var downloadArgs = new DownloadArguments();

            downloadArgs.Username = ArgumentUtils.GetParameter<string>(args, "-username") ?? ArgumentUtils.GetParameter<string>(args, "-user");
            downloadArgs.Password = ArgumentUtils.GetParameter<string>(args, "-password") ?? ArgumentUtils.GetParameter<string>(args, "-pass");
            SteamManager.Config.RememberPassword = ArgumentUtils.HasParameter(args, "-remember-password");

            downloadArgs.AppId = ArgumentUtils.GetParameter<uint>(args, "-app");

            SteamManager.Config.DownloadAllPlatforms = ArgumentUtils.HasParameter(args, "-all-platforms");

            if (ArgumentUtils.HasParameter(args, "-os"))
            {
                downloadArgs.OperatingSystem = ArgumentUtils.GetParameter<string>(args, "-os");
                if (SteamManager.Config.DownloadAllPlatforms && !String.IsNullOrEmpty(downloadArgs.OperatingSystem))
                {
                    Console.WriteLine("Error: Cannot specify -os when -all-platforms is specified.");
                }
            }
            
            downloadArgs.Architecture = ArgumentUtils.GetParameter<string>(args, "-osarch");
            SteamManager.Config.DownloadAllLanguages = ArgumentUtils.HasParameter(args, "-all-languages");

            if (ArgumentUtils.HasParameter(args, "-language"))
            {
                downloadArgs.Language = ArgumentUtils.GetParameter<string>(args, "-language");
                if (SteamManager.Config.DownloadAllLanguages && !String.IsNullOrEmpty(downloadArgs.Language))
                {
                    Console.WriteLine("Error: Cannot specify -language when -all-languages is specified.");
                }
            }
            downloadArgs.LowViolence = ArgumentUtils.HasParameter(args, "-lowviolence");

            return downloadArgs;
        }
    }
}
