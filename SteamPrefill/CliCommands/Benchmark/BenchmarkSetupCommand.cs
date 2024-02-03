// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands.Benchmark
{
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

        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.",
            Converter = typeof(NullableBoolConverter))]
        public bool? NoAnsiEscapeSequences { get; init; }

        [CommandOption("preset",
            Description = "Sets up a benchmark with one or more preset workloads, with differing performance characteristics. Can be SmallChunks/BigChunks",
            Converter = typeof(PresetWorkloadConverter),
            Validators = new[] { typeof(PresetWorkloadValidator) })]
        public IReadOnlyList<PresetWorkload> Presets { get; init; } = new List<PresetWorkload> { };

        private List<uint> PresetAppIds => Presets.Select(e => UInt32.Parse(e)).ToList();

        private IAnsiConsole _ansiConsole;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences ?? true;

            var downloadArgs = new DownloadArguments
            {
                NoCache = AppConfig.NoLocalCache
            };
            using var steamManager = new SteamManager(_ansiConsole, downloadArgs);
            ValidateUserHasSelectedApps(steamManager);

            try
            {
                var combinedAppIds = PresetAppIds.Union(AppIds).ToList();
                await steamManager.InitializeAsync();
                await steamManager.SetupBenchmarkAsync(combinedAppIds, BenchmarkAllOwnedApps ?? false, UseSelectedApps ?? false);
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
            if (Presets.Any())
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