// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    [UsedImplicitly]
    [Command("verify", Description = "Verifies the integrity of the Lancache against the latest game manifests." +
                                      "  Automatically includes apps selected using the 'select-apps' command")]
    public class VerifyCommand : ICommand
    {
        [CommandOption("all", Description = "Verifies all currently owned games", Converter = typeof(NullableBoolConverter))]
        public bool? VerifyAllOwnedGames { get; init; }

        [CommandOption("recent", Description = "Verify will include all games played in the last 2 weeks.", Converter = typeof(NullableBoolConverter))]
        public bool? VerifyRecentGames { get; init; }

        [CommandOption("recently-purchased", Description = "Verify will include all games purchased in the last 2 weeks.", Converter = typeof(NullableBoolConverter))]
        public bool? VerifyRecentlyPurchasedGames { get; init; }

        [CommandOption("top", Description = "Verifies the most popular games by player count, over the last 2 weeks.  Default: 50")]
        public int? VerifyPopularGames
        {
            get => _verifyPopularGames;
            // Need to use a setter in order to set a default value, so that the default will only be used when the option flag is specified
            set => _verifyPopularGames = value ?? 50;
        }

        [CommandOption("os", Description = "Specifies which operating system(s) games should be verified for.  Can be windows/linux/macos",
            Converter = typeof(OperatingSystemConverter), Validators = new[] { typeof(OperatingSystemValidator) })]
        public IReadOnlyList<OperatingSystem> OperatingSystems { get; init; } = new List<OperatingSystem> { OperatingSystem.Windows };

        [CommandOption("verbose", Description = "Produces more detailed log output.", Converter = typeof(NullableBoolConverter))]
        public bool? Verbose
        {
            get => AppConfig.VerboseLogs;
            init => AppConfig.VerboseLogs = value ?? false;
        }

        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoAnsiEscapeSequences { get; init; }

        private IAnsiConsole _ansiConsole;
        private int? _verifyPopularGames;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            await UpdateChecker.CheckForUpdatesAsync(typeof(Program), "tpill90/steam-lancache-prefill", AppConfig.TempDir);

            var downloadArgs = new DownloadArguments
            {
                Force = false, // Verification doesn't need force
                TransferSpeedUnit = TransferSpeedUnit.Bits, // Default
                OperatingSystems = OperatingSystems.ToList()
            };

            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateUserHasSelectedApps(steamManager);
            ValidatePopularGameCount();

            try
            {
                await steamManager.InitializeAsync();
                await steamManager.VerifyMultipleAppsAsync(VerifyAllOwnedGames ?? false,
                                                           VerifyRecentGames ?? false,
                                                           VerifyPopularGames,
                                                           VerifyRecentlyPurchasedGames ?? false);
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

            if ((VerifyAllOwnedGames ?? false) || (VerifyRecentGames ?? false) || (VerifyRecentlyPurchasedGames ?? false) || VerifyPopularGames != null || userSelectedApps.Any())
            {
                return;
            }

            _ansiConsole.MarkupLine(Red("No apps have been selected for verify! At least 1 app is required!"));
            _ansiConsole.MarkupLine(Red($"Use the {Cyan("select-apps")} command to interactively choose which apps to verify. "));
            _ansiConsole.MarkupLine("");
            _ansiConsole.Markup(Red($"Alternatively, the flags {LightYellow("--all")}, {LightYellow("--recent")}, {LightYellow("--recently-purchased")}, or {LightYellow("--top")} can be specified."));
            throw new CommandException(".", 1, true);
        }

        private void ValidatePopularGameCount()
        {
            if (VerifyPopularGames != null && VerifyPopularGames < 1 || VerifyPopularGames > 100)
            {
                _ansiConsole.Markup(Red($"Value for {LightYellow("--top")} must be in the range 1-100"));
                throw new CommandException(".", 1, true);
            }
        }
    }
}
