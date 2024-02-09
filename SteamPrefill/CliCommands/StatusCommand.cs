
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("status", Description = "List all currently selected apps and the used disk space.")]
    public class StatusCommand : ICommand
    {
        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoAnsiEscapeSequences { get; init; }

        [CommandOption("os", Description = "Specifies which operating system(s) games should be downloaded for.  Can be windows/linux/macos",
            Converter = typeof(OperatingSystemConverter), Validators = new[] { typeof(OperatingSystemValidator) })]
        public IReadOnlyList<OperatingSystem> OperatingSystems { get; init; } = new List<OperatingSystem> { OperatingSystem.Windows };

        [CommandOption("sort-order", Description = "Specifies in which way the data should be sorted. Can be ascending/descending",
            Converter = typeof(SortOrderConverter), Validators = new[] { typeof(SortOrderValidator) })]
        public SortOrder SortOrder { get; init; } = SortOrder.Ascending;

        [CommandOption("sort-column", Description = "Specifies by which column the data should be sorted. Can be app/size",
            Validators = new[] { typeof(SortColumnValidator) })]
        public string SortColumn { get; init; } = "app";

        private IAnsiConsole _ansiConsole;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            var downloadArgs = new DownloadArguments
            {
                NoCache = AppConfig.NoLocalCache,
                OperatingSystems = OperatingSystems.ToList()
            };

            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateUserHasSelectedApps(steamManager);

            try
            {
                await steamManager.InitializeAsync();
                await steamManager.CurrentlyDownloadedAsync(SortOrder, SortColumn);
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

        // Validates that the user has selected at least 1 app
        private void ValidateUserHasSelectedApps(SteamManager steamManager)
        {
            var userSelectedApps = steamManager.LoadPreviouslySelectedApps();

            if (!userSelectedApps.Any())
            {
                // User hasn't selected any apps yet
                _ansiConsole.MarkupLine(Red("No apps have been selected for benchmark! At least 1 app is required!"));
                _ansiConsole.Markup(Red($"See flags {LightYellow("--appid")}, {LightYellow("--all")} and {LightYellow("--use-selected")} to interactively choose which apps to prefill"));

                throw new CommandException(".", 1, true);
            }
        }
    }
}