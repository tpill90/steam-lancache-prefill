// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands
{
    //TODO description
    //TODO write documentation
    //TODO split up internal logic so that the prefill and validation logic don't overlap too much
    [UsedImplicitly]
    [Command("validate", Description = "TODO")]
    public class ValidateCommand : ICommand
    {
        [CommandOption("all", Description = "Validates all currently owned games", Converter = typeof(NullableBoolConverter))]
        public bool? DownloadAllOwnedGames { get; init; }

        //TODO implement
        [CommandOption("appid", Description = "TODO")]
        public List<uint> AppIds { get; init; } = new List<uint>();

        //TODO implement this
        [CommandOption("use-selected", Description = "TODO", Converter = typeof(NullableBoolConverter))]
        public bool? UseSelectedApps { get; init; }

        [CommandOption("force", 'f',
            Description = "Forces the validation to always run, overrides the default behavior of only validating if a newer version is available.",
            Converter = typeof(NullableBoolConverter))]
        public bool? Force { get; init; }

        [CommandOption("os", Description = "Specifies which operating system(s) games should be downloaded for.  Can be windows/linux/macos",
            Converter = typeof(OperatingSystemConverter), Validators = new[] { typeof(OperatingSystemValidator) })]
        public IReadOnlyList<OperatingSystem> OperatingSystems { get; init; } = new List<OperatingSystem> { OperatingSystem.Windows };

        [CommandOption("verbose", Description = "Produces more detailed log output. Will output logs for games are already up to date.", Converter = typeof(NullableBoolConverter))]
        public bool? Verbose
        {
            get => AppConfig.VerboseLogs;
            init => AppConfig.VerboseLogs = value ?? default(bool);
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

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            await UpdateChecker.CheckForUpdatesAsync(typeof(Program), "tpill90/steam-lancache-prefill", AppConfig.TempDir);

            var downloadArgs = new DownloadArguments
            {
                Force = Force ?? default(bool),
                TransferSpeedUnit = TransferSpeedUnit,
                OperatingSystems = OperatingSystems.ToList(),
                ValidateChunks = true
            };

            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateUserHasSelectedApps(steamManager);

            try
            {
                await steamManager.InitializeAsync();
                await steamManager.DownloadMultipleAppsAsync(DownloadAllOwnedGames ?? default(bool), default(bool), 0, false);
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

            if ((DownloadAllOwnedGames ?? default(bool)) || userSelectedApps.Any())
            {
                return;
            }

            _ansiConsole.MarkupLine(Red("No apps have been selected for prefill! At least 1 app is required!"));
            _ansiConsole.MarkupLine(Red($"Use the {Cyan("select-apps")} command to interactively choose which apps to prefill. "));
            _ansiConsole.MarkupLine("");
            _ansiConsole.Markup(Red($"Alternatively, the flag {LightYellow("--all")} can be specified to prefill all owned apps"));
            throw new CommandException(".", 1, true);
        }
    }
}
