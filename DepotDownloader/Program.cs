using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

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
    // TODO - Performance - Benchmark allocations, especially during the download.  Write a benchmark.net test for the download stage.
    // TODO - Performance - Finish reducing allocation warnings in DPA.
    // TODO - Performance - Program startup is really slow.  Seems mostly related to logging into Steam. Should log how long certain things take
    // TODO - Performance - Figure out why this app isn't consistently hitting 10gbit download speeds.
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Spectre - Cleanup all instances of AnsiConsole.Console
    // TODO - Logging into steam with expired password fails on first try.  Rerunning works
    // TODO Write logic that only downloads a manifest if it hasnt been downloaded already
    // TODO implement logic that gets a list of all games that a user has, and downloads them
    // TODO implement logic to get a list of games from a user's collection
    // TODO - Document in readme where users can find help with this project
    // TODO - See if WebClient baseUrl can help with the number of allocations
    // TODO - See if UriBuilder can help w\ allocations
    // TODO - Run dotnet format on this program
    // TODO - Design a "benchmark mode" that runs a single/multiple applications in a loop.  If a game's manifests already exist, then there won't even be a need to login to Steam.
    // TODO - Exceptions being thrown causes the application to hang.
    // TODO - Remove spectre.console.xml from publish package
    // TODO - Consider adding a flag for not saving manifest cache.  Measure how much the manifest cache could actually end up being after a large # of installs
    // TODO setup build
    // TODO setup publish script
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
