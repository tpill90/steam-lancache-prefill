namespace SteamPrefill
{
    /* TODO
     * Research -Determine if there is a way to selectively delete games from the cache, so that I don't have to nuke my whole cache to test something.
     * Testing - Should invest some time into adding unit tests
     * Cleanup Resharper code issues, and github code issues.
     * Cleanup TODOs
     * Build - Fail build on trim warnings
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