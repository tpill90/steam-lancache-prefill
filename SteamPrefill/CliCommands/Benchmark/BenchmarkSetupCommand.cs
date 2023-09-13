// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands.Benchmark
{
    //TODO add preset for destiny and dota

    [UsedImplicitly]
    [Command("benchmark setup", Description = "Configures a benchmark workload from multiple apps.  Benchmark workload is static, and portable between machines.")]
    public class BenchmarkSetupCommand : ICommand
    {
        [CommandOption("appid", Description = "The id of one or more apps to include in benchmark workload file.  AppIds can be found using https://steamdb.info/")]
        public List<uint> AppIds { get; init; } = new List<uint>();

        [CommandOption("all", Description = "Includes all currently owned apps in benchmark workload file", Converter = typeof(NullableBoolConverter))]
        public bool? BenchmarkAllOwnedApps { get; init; }

        [CommandOption("use-selected", Description = "Includes apps selected using 'select-apps' in the benchmark workload file", Converter = typeof(NullableBoolConverter))]
        public bool? UseSelectedApps { get; init; }

        [CommandOption("nocache",
            Description = "Skips using locally cached manifests.  Saves disk space, at the expense of slower subsequent runs.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoLocalCache { get; init; }

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

            var downloadArgs = new DownloadArguments
            {
                NoCache = NoLocalCache ?? false
            };
            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);

            ValidateUserHasSelectedApps(steamManager);

            try
            {
                await steamManager.InitializeAsync();
                await steamManager.SetupBenchmarkAsync(AppIds.ToList(), BenchmarkAllOwnedApps ?? false, UseSelectedApps ?? false);
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

            if (AppIds != null && AppIds.Any())
            {
                return;
            }
            if (BenchmarkAllOwnedApps ?? false)
            {
                return;
            }
            if (UseSelectedApps != null && UseSelectedApps.Value)
            {
                if (userSelectedApps.Any())
                {
                    return;
                }

                // User hasn't selected any apps yet, despite them specifying the --use-selected flag
                _ansiConsole.MarkupLine(Red($"Flag {LightYellow("--use-selected")} was specified, however no apps have been previously selected with {Cyan("select-apps")}."));
                _ansiConsole.Markup(Red("Please choose some apps and try again"));

                throw new CommandException(".", 1, true);
            }

            _ansiConsole.MarkupLine(Red("No apps have been selected for benchmark! At least 1 app is required!"));
            _ansiConsole.Markup(Red($"See flags {LightYellow("--appid")}, {LightYellow("--all")} and {LightYellow("--use-selected")} to interactively choose which apps to prefill"));

            throw new CommandException(".", 1, true);
        }
    }
}