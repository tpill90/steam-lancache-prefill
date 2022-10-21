namespace SteamPrefill
{
    /* TODO
     * Research - See if this endpoint can give me anything useful in some way.  Its also referenced in SteamKit2's PICSProductInfo class. clientconfig.akamai.steamstatic.com/appinfo/17390/sha/896ef0a3ad4c4901b78916c76e85cde05cf5f137.txt.gz
     * Determine if there is a way to selectively delete games from the cache, so that I don't have to nuke my whole cache to test something.
     *
     * Documentation - Add to readme how you can login to multiple accounts.  Either two folders with two copies of the app, or setup family sharing.
     * Documentation - Add to docs how exactly passwords/credentials are used, and stored.
     * Documentation - Steam family sharing is supported.  You can even prefill games while on another machine.  Should probably add this to the readme
     * Documentation - The readme could probably use a little bit of care.  Some of the images are way too large/wide.  Some images are not crisp and suffer from display scaling
     *
     * Testing - Should invest some time into adding unit tests
     * Cleanup warnings, resharper code issues, and github code issues.
     * Cleanup TODOs
     * Build - Fail build on both warnings + trim warnings
     * Update to dotnet 7 sdk + dotnet 7 target
     * Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
     */
    public static class Program
    {
        public static async Task<int> Main()
        {
            try
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
            //TODO dedupe this throughout the codebase
            catch (TimeoutException e)
            {
                AnsiConsole.Console.MarkupLine("\n");
                if (e.StackTrace.Contains(nameof(UserAccountStore.GetUsernameAsync)))
                {
                    AnsiConsole.Console.MarkupLine(Red("Timed out while waiting for username entry"));
                }
                if (e.StackTrace.Contains(nameof(AnsiConsoleExtensions.ReadPassword)))
                {
                    AnsiConsole.Console.MarkupLine(Red("Timed out while waiting for password entry"));
                }
                AnsiConsole.Console.WriteException(e, ExceptionFormats.ShortenPaths);
            }
            catch (Exception e)
            {
                AnsiConsole.Console.WriteException(e, ExceptionFormats.ShortenPaths);
            }

            return 0;
        }

        public static class OperatingSystem
        {
            public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
    }
}