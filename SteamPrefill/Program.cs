namespace SteamPrefill
{
    /* TODO
     * Documentation - Install instructions.  Possibly add the wget + unzip command as well for linux users?
     * FAQ - Add an entry that it is possible to run this on a schedule, by using a cron job.  Have an example of the command.
     * Logout - Should catch and handle ctrl+c keypress, to gracefully shutdown Steam before terminating
     * Login - Seems like 2fa isn't saving login info, possibly only when doing select-apps
     * Select-apps - Prefilling with select apps defaults to bytes not bits
     * Docs - Update useful links in docs, add them to a table, with notes.
     * In LancacheIpResolver.cs, change 127.0.0.1 over to say 'localhost' instead.
     * Logout - Should catch and handle ctrl+c keypress, to gracefully shutdown Steam before terminating
     * Determine if its possible to detect ipv6, and display a message to the user that ipv6 is not supported
     * Documentation - Update useful links in docs, add them to a table, with notes.
     * Documentation - Add to readme how you can login to multiple accounts.  Either two folders with two copies of the app, or setup family sharing.
     * Documentation - Add to docs how exactly passwords/credentials are used, and stored.
     * Documentation - Steam family sharing is supported.  You can even prefill games while on another machine.  Should probably add this to the readme
     * Documentation - The readme could probably use a little bit of care.  Some of the images are way too large
     * Testing - Should invest some time into adding unit tests
     * Bug - Should the CdnPool get multiple regions, so that when it fails it tries to check other distinct CDNs.  Maybe this will improve reliability?
     * Cleanup warnings, resharper code issues, and github code issues
     * Build - Fail build on both warnings + trim warnings
     * Update to dotnet 7 sdk + dotnet 7 target
     * Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
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