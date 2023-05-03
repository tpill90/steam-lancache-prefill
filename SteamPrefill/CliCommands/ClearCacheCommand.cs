// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("clear-cache", Description = "Empties out temporary cached data, to free up disk space")]
    public sealed class ClearCacheCommand : ICommand
    {
        [CommandOption("yes", shortName: 'y', Description = "When specified, will clear the cache without prompting", Converter = typeof(NullableBoolConverter))]
        public bool? AcceptPrompt { get; init; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            var filesShouldBeDeleted = AcceptPrompt ?? false;

            var ansiConsole = console.CreateAnsiConsole();
            // Remove the v1/v2/v3 sub-directories
            var rootCacheDir = new DirectoryInfo(AppConfig.CacheDir).Parent;

            // Scanning the cache directory to see how much space could be saved
            List<FileInfo> cacheFolderContents = null;
            ansiConsole.StatusSpinner().Start($"Scanning {Cyan("Cache")} directory...", ctx =>
            {
                cacheFolderContents = rootCacheDir.EnumerateFiles("*.*", SearchOption.AllDirectories).ToList();
            });

            var totalSizeOnDisk = ByteSize.FromBytes(cacheFolderContents.Sum(e => e.Length));
            if (totalSizeOnDisk.Bytes == 0 && cacheFolderContents.Count == 0)
            {
                ansiConsole.LogMarkupLine($"Nothing to cleanup! {Cyan("Cache")} directory is already empty!");
                return default;
            }
            ansiConsole.LogMarkupLine($"Found {LightYellow(cacheFolderContents.Count)} cached files, totaling {Magenta(totalSizeOnDisk.ToDecimalString())}");

            // If user hasn't passed in the accept flag, then we should ask them if they want to delete the files
            if (!(AcceptPrompt ?? false))
            {
                var userResponse = ansiConsole.Prompt(new SelectionPrompt<bool>()
                                                      .Title("Continue to empty cache?")
                                                      .AddChoices(true, false)
                                                      .UseConverter(e => e == false ? "No" : "Yes"));
                filesShouldBeDeleted = filesShouldBeDeleted || userResponse;
            }

            if (!filesShouldBeDeleted)
            {
                return default;
            }

            ansiConsole.StatusSpinner().Start("Deleting cached files...", ctx =>
            {
                Directory.Delete(rootCacheDir.FullName, true);
            });
            ansiConsole.LogMarkupLine("Done!");

            return default;
        }
    }
}