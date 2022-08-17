using System.Threading.Tasks;
using CliFx;
using SteamPrefill.Utils;

namespace SteamPrefill
{
    /* TODO
     * Add Prefill.Common project
     * Make sure changes done in battlenet prefill are done here
     * Update to dotnet 7 sdk + dotnet 7 target
     * Setup global using file
     * Update resharper dotsettings file to match Battlenet Prefill
     * Replace utf8json with System.Text.Json + Source generator
     * Unparallelize the publish script, take it from BattleNetPrefill
     * Remove Spectre.Console as a submodule, and add precompiled binary as a reference
     * Docs - Add to readme how you can login to multiple accounts.  Either two folders with two copies of the app, or setup family sharing.
     * Docs - Add an image to the main heading in the readme.  That way people have an image that they can immediately see when they come to the repo
     * Would it be useful to port the LancacheIpResolver over into its own app, in order to replace the defunct lancache diagnostics/litmus test
     * Documentation - Steam family sharing is supported.  You can even prefill games while on another machine.  Should probably add this to the readme
     * Bug - TryWaitForLoginKey() is flaky.  Steam doesn't seem to be saving credentials properly in australia.
     *       Keeps requiring password reentry.  Other users report the same
     *       What happens if Lancache is completely disabled?  Do users stay logged in?
     *       Is there a limit to the # of computers that can be logged in at the same time?  Test logging in with multiple VMs for the same account
     * Bug - Should the CdnPool get multiple regions, so that when it fails it tries to check other distinct CDNs.  Maybe this will improve reliability?
     * Feature - Could it be easier to select multiple apps on the select screen, without having to press down + space repeatedly?  
                      Possibly by holding spacebar + hitting down arrow -> keep selecting as you hit down arrow
     * Build - Fail build on both warnings + trim warnings
     * Deprecation - Remove --dns-override in a future version.
     * Documentation - Explain process for updating app
     * General - Promote this app on r/lanparty.
     *
     * Feature - Design a "benchmark mode" that runs a single/multiple applications in a loop.  If a game's manifests already exist, then there won't even be a need to login to Steam.
     * Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
     * CliFx - I wish there was a way to color the help text output from CliFx.  Everything is so flat, and cant draw attention to important parts
     */
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
