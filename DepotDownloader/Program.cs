using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace DepotDownloader
{
    // TODO - Tech Debt - Rename Solution/csproj/etc from DepotDownloader to SteamPrefill
    // TODO - Tech Debt - Cleanup Console.WriteLine calls
    // TODO - Tech Debt - Run dotnet format on this codebase
    // TODO - Features - Start implementing features from lancache-autofill https://github.com/zeropingheroes/lancache-autofill
    // TODO - Features - Write check to see if a newer version is available, and display a message to the user if there is
    // TODO - Features - Implement a --force flag, that will force the app to always re-download the depots
    // TODO - Features - Design a "benchmark mode" that runs a single/multiple applications in a loop.  If a game's manifests already exist, then there won't even be a need to login to Steam.
    // TODO - Build - Add publish script
    // TODO - Build - Remove spectre.console.xml from publish package
    // TODO - Build - Setup build using Github actions.  Should simply compile the application
    // TODO - Documentation - Thoroughly document usage of this app in readme.md
    // TODO - Documentation - Include reasons as to why this program is better than the one it replaces.  Download speed, no dependencies, no disk writes.
    // TODO - Documentation - Update documentation to show why you should be using UTF16.  Include an image showing before/after.  Also possibly do a check on startup?
    // TODO - Documentation - Document in readme where users can find help with this project
    // TODO - Performance - Benchmark allocations, especially during the download.  Write a benchmark.net test for the download stage.
    // TODO - Performance - Finish reducing allocation warnings in DPA.
    // TODO - Performance - Program startup is really slow.  Seems mostly related to logging into Steam. Should log how long certain things take
    // TODO - Performance - Figure out why this app isn't consistently hitting 10gbit download speeds.
    // TODO - Spectre - Cleanup all instances of AnsiConsole.Console
    // TODO implement logic that gets a list of all games that a user has, and downloads them
    // TODO implement logic to get a list of games from a user's collection
    // TODO - Exceptions being thrown causes the application to hang.
    // TODO - Consider adding a flag for not saving manifest cache.  Measure how much the manifest cache could actually end up being after a large # of installs
    // TODO - Add a command that clears out the manifest cache, and displays how much data was saved
    // TODO - General - Promote this app on r/lanparty and discord once it is finished.
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Metrics - Setup and configure Github historical statistics (Downloads, page views, etc).  This will be useful for seeing project usage.
    // TODO Test this app on Linux to ensure that it works correctly
    // TODO - Rewrite the repo's original init commit
    // TODO - Test out https://github.com/microsoft/infersharpaction
    // TODO - Things I want tested :  Does initial download max your internet speed?  Does cached downloads max your lan speed?
    // TODO - Add a benchmark for web request allocations, and optimize
    // TODO - Add a command that lists all available games for a user, and allows them to select which ones to download
    // TODO - Should there be an option to configure your username, so that you don't need to pass it in all the time?
    // TODO - should probably add file system logging, so that I can help people diagnose why things arent working for them
    public class Program
    {
        public static async Task<int> Main()
        {
            var cliBuilder = new CliApplicationBuilder()
                             .AddCommandsFromThisAssembly()
                             .SetTitle("SteamPrefill")
                             .SetExecutableName("SteamPrefill");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cliBuilder.SetExecutableName("SteamPrefill.exe");
            }
            return await cliBuilder
                         .Build()
                         .RunAsync();
        }
    }
}
