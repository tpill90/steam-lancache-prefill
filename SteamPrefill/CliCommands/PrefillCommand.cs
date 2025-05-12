// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("prefill", Description = "Downloads the latest version of one or more specified app(s)." +
                                           "  Automatically includes apps selected using the 'select-apps' command")]
    public class PrefillCommand : ICommand
    {
        [CommandOption("all", Description = "Prefills all currently owned games", Converter = typeof(NullableBoolConverter))]
        public bool? DownloadAllOwnedGames { get; init; }

        [CommandOption("recent", Description = "Prefill will include all games played in the last 2 weeks.", Converter = typeof(NullableBoolConverter))]
        public bool? PrefillRecentGames { get; init; }

        [CommandOption("recently-purchased", Description = "Prefill will include all games purchased in the last 2 weeks.", Converter = typeof(NullableBoolConverter))]
        public bool? PrefillRecentlyPurchased { get; init; }

        [CommandOption("top", Description = "Prefills the most popular games by player count, over the last 2 weeks.  Default: 50")]
        public int? PrefillPopularGames
        {
            get => _prefillPopularGames;
            // Need to use a setter in order to set a default value, so that the default will only be used when the option flag is specified
            set => _prefillPopularGames = value ?? 50;
        }

        [CommandOption("force", 'f',
            Description = "Forces the prefill to always run, overrides the default behavior of only prefilling if a newer version is available.",
            Converter = typeof(NullableBoolConverter))]
        public bool? Force { get; init; }

        [CommandOption("os", Description = "Specifies which operating system(s) games should be downloaded for.  Can be windows/linux/macos",
            Converter = typeof(OperatingSystemConverter), Validators = new[] { typeof(OperatingSystemValidator) })]
        public IReadOnlyList<OperatingSystem> OperatingSystems { get; init; } = new List<OperatingSystem> { OperatingSystem.Windows };

        [CommandOption("verbose", Description = "Produces more detailed log output. Will output logs for games are already up to date.", Converter = typeof(NullableBoolConverter))]
        public bool? Verbose
        {
            get => AppConfig.VerboseLogs;
            init => AppConfig.VerboseLogs = value ?? false;
        }

        [CommandOption("unit",
            Description = "Specifies which unit to use to display download speed.  Can be either bits/bytes.",
            Converter = typeof(TransferSpeedUnitConverter))]
        public TransferSpeedUnit TransferSpeedUnit { get; init; } = TransferSpeedUnit.Bits;

        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoAnsiEscapeSequences { get; init; }

        private IAnsiConsole _ansiConsole;
        private int? _prefillPopularGames;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            await UpdateChecker.CheckForUpdatesAsync(typeof(Program), "tpill90/steam-lancache-prefill", AppConfig.TempDir);

            var downloadArgs = new DownloadArguments
            {
                Force = Force ?? false,
                TransferSpeedUnit = TransferSpeedUnit,
                OperatingSystems = OperatingSystems.ToList()
            };

            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateUserHasSelectedApps(steamManager);
            ValidatePopularGameCount();

            try
            {
                await steamManager.InitializeAsync();
                await steamManager.DownloadMultipleAppsAsync(DownloadAllOwnedGames ?? false,
                                                             PrefillRecentGames ?? false,
                                                             PrefillPopularGames,
                                                             PrefillRecentlyPurchased ?? false);
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

            if ((DownloadAllOwnedGames ?? false) || (PrefillRecentGames ?? false) || (PrefillRecentlyPurchased ?? false) || PrefillPopularGames != null || userSelectedApps.Any())
            {
                return;
            }

            _ansiConsole.MarkupLine(Red("No apps have been selected for prefill! At least 1 app is required!"));
            _ansiConsole.MarkupLine(Red($"Use the {Cyan("select-apps")} command to interactively choose which apps to prefill. "));
            _ansiConsole.MarkupLine("");
            _ansiConsole.Markup(Red($"Alternatively, the flags {LightYellow("--all")}, {LightYellow("--recent")}, {LightYellow("--recently-purchased")}, or {LightYellow("--top")} can be specified."));
            throw new CommandException(".", 1, true);
        }

        private void ValidatePopularGameCount()
        {
            if (PrefillPopularGames != null && PrefillPopularGames < 1 || PrefillPopularGames > 100)
            {
                _ansiConsole.Markup(Red($"Value for {LightYellow("--top")} must be in the range 1-100"));
                throw new CommandException(".", 1, true);
            }
        }
    }
}
