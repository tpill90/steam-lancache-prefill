using System.Threading.Tasks;
using CliFx;
using SteamPrefill.Utils;

namespace SteamPrefill
{
    /* TODO
     * Documentation - Update documentation to show why you should be using UTF16.
     *                 The Master is having issues with getting it working on his machine, maybe he could help me with the docs.
     *                 Include an image showing before/after.  Also possibly do a check on startup?
     */
    // 
    // TODO - Bug - Steam doesn't seem to be saving credentials properly in australia.  Keeps requiring password reentry.  Other users report the same
    // TODO - Build - Fail build on both warnings + trim warnings
    // TODO - Documentation - Explain process for updating app

    // TODO - Should probably add file system logging, so that I can help people diagnose why things arent working for them
    // TODO - General - Promote this app on r/lanparty and discord once it is finished.
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - I wish there was a way to color the help text output from CliFx.  Everything is so flat, and cant draw attention to important parts
    // TODO - Test out https://github.com/microsoft/infersharpaction
    // TODO - Performance - Finish reducing allocation warnings in DPA.
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
