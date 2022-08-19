namespace SteamPrefill
{
    /* TODO
     * Select apps - Cleanup dedicated servers + just cause multiplayer mod + tloader + red orchestra community maps from being displayed
     * Documentation - Add entry to FAQ explaining that it is possible to run this app on the server, and explain how to do that.
     * Cleanup some apps that shouldnt be shown, like Red Orchestra community maps + dedicated server.  Killing Floor Server
     *
     * Docs - Add to readme how you can login to multiple accounts.  Either two folders with two copies of the app, or setup family sharing.
     * Documentation - Steam family sharing is supported.  You can even prefill games while on another machine.  Should probably add this to the readme
     * Update resharper dotsettings file to match Battlenet Prefill
     * Docs - Add an image to the main heading in the readme.  That way people have an image that they can immediately see when they come to the repo
     * Bug - Should the CdnPool get multiple regions, so that when it fails it tries to check other distinct CDNs.  Maybe this will improve reliability?
     * Feature - Could it be easier to select multiple apps on the select screen, without having to press down + space repeatedly?  
                      Possibly by holding spacebar + hitting down arrow -> keep selecting as you hit down arrow
     * Build - Fail build on both warnings + trim warnings
     * Deprecation - Remove --dns-override in a future version.
     
    * Update to dotnet 7 sdk + dotnet 7 target
     * General - Promote this app on r/lanparty.
     * Update readme to include table of contents.  Make sure the formatting is similar to battlenet prefill
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

        public static class OperatingSystem
        {
            public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
}
