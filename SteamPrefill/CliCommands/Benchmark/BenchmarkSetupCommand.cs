// ReSharper disable MemberCanBePrivate.Global - CommandOption properties can't ever be private, otherwise they won't work with CliFx.
// ReSharper disable UnusedAutoPropertyAccessor.Global - Init setters are used even if resharper thinks they aren't, since CliFx sets them at runtime.
namespace SteamPrefill.CliCommands.Benchmark
{
    [UsedImplicitly]
    [Command("benchmark setup", Description = "Configures a benchmark workload from multiple apps.  Benchmark workload is static, and portable between machines.")]
    public class BenchmarkSetupCommand : ICommand
    {
        [CommandOption("appid", Description = "The id of one or more apps to include in benchmark workload file.  AppIds can be found using https://steamdb.info/")]
        public List<uint> AppIds { get; init; } = new List<uint>();

        [CommandOption("all", Description = "Includes all currently owned apps in benchmark workload file")]
        public bool BenchmarkAllOwnedApps { get; init; }

        [CommandOption("use-selected", Description = "Includes apps selected using 'select-apps' in the benchmark workload file")]
        public bool UseSelectedApps { get; init; }

        [CommandOption("no-ansi",
            Description = "Application output will be in plain text.  " +
                          "Should only be used if terminal does not support Ansi Escape sequences, or when redirecting output to a file.")]
        public bool NoAnsiEscapeSequences { get; init; }

        [CommandOption("preset",
            Description = "Sets up a benchmark with one or more preset workloads, with differing performance characteristics. Can be SmallChunks/LargeChunks",
            Converter = typeof(PresetWorkloadConverter),
            Validators = new[] { typeof(PresetWorkloadValidator) })]
        public IReadOnlyList<PresetWorkload> Presets { get; init; } = new List<PresetWorkload> { };

        private List<uint> PresetAppIds => Presets.Select(e => UInt32.Parse(e)).ToList();

        private IAnsiConsole _ansiConsole;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();
            // Property must be set to false in order to disable ansi escape sequences
            _ansiConsole.Profile.Capabilities.Ansi = !NoAnsiEscapeSequences;

            using var steamManager = new SteamManager(_ansiConsole, new DownloadArguments());
            ValidateUserHasSelectedApps(steamManager);

            try
            {
                var combinedAppIds = PresetAppIds.Union(AppIds).ToList();
                await steamManager.InitializeAsync();
                await steamManager.SetupBenchmarkAsync(combinedAppIds, BenchmarkAllOwnedApps, UseSelectedApps);
            }
            finally
            {
                steamManager.Shutdown();
            }
        }

        // Validates that the user has selected at least 1 app
        private void ValidateUserHasSelectedApps(SteamManager steamManager)
        {
            var selectedApps = steamManager.LoadPreviouslySelectedApps();

            if (AppIds != null && AppIds.Any())
            {
                return;
            }
            if (Presets.Any())
            {
                return;
            }
            if (BenchmarkAllOwnedApps)
            {
                return;
            }
            if (UseSelectedApps)
            {
                if (selectedApps.Any())
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