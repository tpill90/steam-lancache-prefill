using System.Threading.Tasks;
using CliFx;
using SteamPrefill.Utils;

namespace SteamPrefill
{
    // TODO - Maybe --dns-override should just automatically point to 127.0.0.1, and rename it to something else.  Could I just check to see if a lancache is hosted at 127.0.0.1?
    // TODO - Measure performance, and improve the app startup time for a large # of games (1000+)
    // TODO - Bug - Steam doesn't seem to be saving credentials properly in australia.  Keeps requiring password reentry.  Other users report the same
    // TODO - Build - Fail build on both warnings + trim warnings
    // TODO - Build - Setup build using Github actions.  Should simply compile the application
    // TODO - Documentation - Explain process for updating app
    // TODO - Feature - Consider adding a flag for not saving manifest cache.  
    // TODO - Feature - Add a command that clears out the manifest cache, and displays how much data was saved
    // TODO - Tech debt - Do I need both Utf8Json and protobuf-net for serialization?

    // TODO - Should probably add file system logging, so that I can help people diagnose why things arent working for them
    // TODO - General - Promote this app on r/lanparty and discord once it is finished.
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - I wish there was a way to color the help text output from CliFx.  Everything is so flat, and cant draw attention to important parts
    // TODO - Test out https://github.com/microsoft/infersharpaction
    // TODO - documentation - improve formatting of readme.md
    // TODO - Documentation - Update documentation to show why you should be using UTF16.  Include an image showing before/after.  Also possibly do a check on startup?
    // TODO - Performance - Benchmark allocations, especially during the download.  Write a benchmark.net test for the download stage.
    // TODO - Performance - Finish reducing allocation warnings in DPA.
    // TODO - Performance - Figure out why this app isn't consistently hitting 10gbit download speeds.
    // TODO - Performance - See if grouping requests in a manner similar to battlenet-prefill (small/large groups) can help with overall download performance
    // TODO - Accuracy - Should figure out some way to verify that my app is completely downloading the games, in the exact same way steam does
    // TODO - Feature - Could it be easier to select multiple apps on the select screen, without having to press down + space repeatedly?  
    //                  Possibly by holding spacebar + hitting down arrow -> keep selecting as you hit down arrow
    // TODO - Should the CdnPool get multiple regions, so that when it fails it tries to check other distinct CDNs.  Maybe this will improve reliability?
    // TODO - Features - Design a "benchmark mode" that runs a single/multiple applications in a loop.  If a game's manifests already exist, then there won't even be a need to login to Steam.
    public static class Program
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
