// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("clear-temp", Description = "Empties out temporary data, such as saved manifests, to free up disk space")]
    public sealed class ClearTempCommand : ICommand
    {
        [CommandOption("yes", shortName: 'y', Description = "When specified, will clear the temp files without prompting.", Converter = typeof(NullableBoolConverter))]
        public bool? AcceptPrompt { get; init; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            var filesShouldBeDeleted = AcceptPrompt ?? false;

            var ansiConsole = console.CreateAnsiConsole();
            // Remove the v1/v2/v3 sub-directories
            var rootTempDir = new DirectoryInfo(AppConfig.TempDir).Parent;

            // Scanning the temp directory to see how much space could be saved
            List<FileInfo> tempFolderContents = null;
            ansiConsole.StatusSpinner().Start($"Scanning {Cyan("temp")} directory...", ctx =>
            {
                tempFolderContents = rootTempDir.EnumerateFiles("*.*", SearchOption.AllDirectories).ToList();
            });

            var totalSizeOnDisk = ByteSize.FromBytes(tempFolderContents.Sum(e => e.Length));
            if (totalSizeOnDisk.Bytes == 0 && tempFolderContents.Count == 0)
            {
                ansiConsole.LogMarkupLine($"Nothing to cleanup! {Cyan("temp")} directory is already empty!");
                return default;
            }
            ansiConsole.LogMarkupLine($"Found {LightYellow(tempFolderContents.Count)} temp files, totaling {Magenta(totalSizeOnDisk.ToDecimalString())}");

            // If user hasn't passed in the accept flag, then we should ask them if they want to delete the files
            if (!(AcceptPrompt ?? false))
            {
                var userResponse = ansiConsole.Prompt(new SelectionPrompt<bool>()
                                                      .Title("Continue to delete temp files?")
                                                      .AddChoices(true, false)
                                                      .UseConverter(e => e == false ? "No" : "Yes"));
                filesShouldBeDeleted = filesShouldBeDeleted || userResponse;
            }

            if (!filesShouldBeDeleted)
            {
                return default;
            }

            ansiConsole.StatusSpinner().Start("Deleting temp files...", ctx =>
            {
                Directory.Delete(rootTempDir.FullName, true);
            });
            ansiConsole.LogMarkupLine("Done!");

            return default;
        }
    }
}