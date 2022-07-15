using System.Threading.Tasks;
using CliFx;
using SteamPrefill.Utils;

namespace SteamPrefill
{
    /* TODO
     **** Pre-release task list ****     
     * Build - Add publish script
     * Build - Remove spectre.console.xml from publish package
     * Choose an appropriate license
     * Documentation - Add a link + badge for LanCache discord, and possibly link to the specific channel used for this app.
     * Documentation - Document in readme where users can find help with this project
     * Documentation - Thoroughly document usage of this app in readme.md.  Include images + gifs where possible.
     * Documentation - Include reasons as to why this program is better than the one it replaces.
     *                 Download speed, no dependencies, no disk writes, no api key, no steamcmd, 2fa support.
     * Tech debt - Rewrite the repo's original init commit
     * Testing - Rerun through whole app one more time, especially after running trim.  Test on linux as well
     * Testing - Re-test cached download speed, see what apps can't hit 10gbps.
     * Testing - Validate update check is working, after uploading an intial version
     * Feedback - Some of the things I am interested in hearing from users:
     *            - Do initial downloads max your internet speed? 1gbps+ would be a good test.  Destiny 2 is a good one to try
     *            - Do cached downloads max your lan speed?  Destiny 2 is a good one to try
     *            - How are initial download speeds overseas?  Only have tested from East Coast US
     *            - Is the application easy to understand + use?  With and without reading the documentation?
     *            - Any other feedback?
     */

    // TODO - Tech Debt - Cleanup all warnings
    // TODO - Tech Debt - Cleanup trim warnings
    // TODO - Build - Fail build on both warnings + trim warnings
    // TODO - Tech debt - Do I need both Utf8Json and protobuf-net for serialization?
    // TODO - Feature - Implement support for filtering by language/operating system/architecture
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
