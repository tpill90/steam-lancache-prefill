using System.Threading.Tasks;
using CliFx;
using SteamPrefill.Utils;

namespace SteamPrefill
{
    // TODO - Tech Debt - Cleanup all warnings
    // TODO - Tech Debt - Cleanup trim warnings
    // TODO - Build - Fail build on both warnings + trim warnings
    // TODO - Tech debt - Do I need both Utf8Json and protobuf-net for serialization?
    // TODO - Feature - Implement support for filtering by language/operating system/architecture
    // TODO - Feature - On the 'select-apps' multi-select, consider implementing the ability to type a letter and skip to games starting with that.
    // TODO - Feature - On the 'select-apps' multi-select, consider adding the ability to filter by typing in a query.
    // TODO - Metrics - Setup and configure Github historical statistics (Downloads, page views, etc).  This will be useful for seeing project usage.
    // TODO - Documentation - Update documentation to show why you should be using UTF16.  Include an image showing before/after.  Also possibly do a check on startup?
    // TODO - Performance - Benchmark allocations, especially during the download.  Write a benchmark.net test for the download stage.
    // TODO - Performance - Finish reducing allocation warnings in DPA.
    // TODO - Performance - Figure out why this app isn't consistently hitting 10gbit download speeds.
    // TODO - Feature - Consider adding a flag for not saving manifest cache.  
    // TODO - Feature - Add a command that clears out the manifest cache, and displays how much data was saved
    // TODO - General - Promote this app on r/lanparty and discord once it is finished.
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - should probably add file system logging, so that I can help people diagnose why things arent working for them
    // TODO - Features - Consider which features from lancache-autofill should be ported over to this app - https://github.com/zeropingheroes/lancache-autofill
    // TODO - Features - Design a "benchmark mode" that runs a single/multiple applications in a loop.  If a game's manifests already exist, then there won't even be a need to login to Steam.
    // TODO - Build - Setup build using Github actions.  Should simply compile the application
    // TODO - I wish there was a way to color the help text output from CliFx.  Everything is so flat, and cant draw attention to important parts
    // TODO - Test out https://github.com/microsoft/infersharpaction
    // TODO - Document process for updating app
    // TODO - fix historical github stats metrics build failing
    // TODO - documentation - improve formatting of readme.md
    public class Program
    {
        public static async Task<int> Main()
        {
            var description = "Automatically fills a Lancache with games from Steam, so that subsequent downloads will be \n" +
                              "  served from the Lancache, improving speeds and reducing load on your internet connection. \n" +
                              "\n" +
                              "  Start by selecting apps for prefill with the 'select-apps' command, then start the prefill using 'prefill'";
            return await new CliApplicationBuilder()
                         .AddCommandsFromThisAssembly()
                         .SetTitle("SteamPrefill")
                         .SetExecutableName($"SteamPrefill{(OperatingSystem.IsWindows() ? ".exe" : "")}")
                         .SetDescription(description)
                         .Build()
                         .RunAsync();
        }
    }
}
