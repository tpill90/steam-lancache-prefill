// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands.Benchmark
{
    [UsedImplicitly]
    [Command("benchmark run", Description = "Runs a benchmark using the previously configured workload.")]
    public class BenchmarkRunCommand : ICommand
    {
        [CommandOption("concurrency", 'c', Description = "Specifies the maximum number of concurrent requests at any one time.")]
        public uint MaxConcurrency { get; init; } = 50;

        [CommandOption("iterations", 'i', Description = "Specifies how many benchmark iterations to run.")]
        public uint MaxRuns { get; init; } = 5;

        [CommandOption("unit",
            Description = "Specifies which unit to use to display download speed.  Can be either bits/bytes.",
            Converter = typeof(TransferSpeedUnitConverter))]
        public TransferSpeedUnit TransferSpeedUnit { get; init; } = TransferSpeedUnit.Bits;

        private IAnsiConsole _ansiConsole;

        private CdnPool _cdnPool;
        private List<QueuedRequest> _allRequests;
        private ByteSize _totalDownloadSize = ByteSize.MinValue;
        private DownloadHandler _downloadHandler;

        public async ValueTask ExecuteAsync(IConsole console)
        {
            _ansiConsole = console.CreateAnsiConsole();

            if (!File.Exists(AppConfig.BenchmarkWorkloadPath))
            {
                _ansiConsole.Markup(Red($"Required benchmark workload file not found!  To configure one for use with this command, use {LightYellow("'benchmark setup'")}"));
                throw new CommandException(".", 3);
            }

            _ansiConsole.LogMarkupLine("Starting benchmark...");

            await InitializeAsync();
            await RunWarmupAsync();

            var runResults = new List<Stopwatch>();
            for (int run = 1; run <= MaxRuns; run++)
            {
                // Force GC between runs, to minimize leftover objects from the previous run.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                var downloadTimer = Stopwatch.StartNew();
                await _ansiConsole.CreateSpectreProgress(TransferSpeedUnit).StartAsync(async ctx =>
                {
                    var downloadArguments = new DownloadArguments { MaxConcurrentRequests = (int)MaxConcurrency };
                    await _downloadHandler.AttemptDownloadAsync(ctx, "Downloading", _allRequests, downloadArguments);
                });
                downloadTimer.Stop();

                // Logging some metrics about the download
                _ansiConsole.LogMarkupLine($"Run {Cyan(run)} finished in {LightYellow(downloadTimer.FormatElapsedString())} - {Magenta(_totalDownloadSize.CalculateBitrate(downloadTimer))}");
                runResults.Add(downloadTimer);
            }

            PrintSummary(runResults, _totalDownloadSize);
        }

        private async Task InitializeAsync()
        {
            var initTimer = Stopwatch.StartNew();
            _ansiConsole.StatusSpinner().Start("Initializing benchmark", _ =>
            {
                var benchmarkWorkload = BenchmarkWorkload.LoadFromFile(AppConfig.BenchmarkWorkloadPath);

                _cdnPool = new CdnPool(_ansiConsole, benchmarkWorkload.CdnServerList);
                _totalDownloadSize = benchmarkWorkload.TotalDownloadSize;

                // Randomizing request order, to simulate a more "realistic" workload similar to Lan Party traffic.
                // We also want to avoid sequential reads, as they might be getting cached by the server's ram
                _allRequests = benchmarkWorkload.AllQueuedRequests;
                _allRequests.Shuffle();
            });

            _downloadHandler = new DownloadHandler(_ansiConsole, _cdnPool);
            await _downloadHandler.InitializeAsync();

            _ansiConsole.LogMarkupLine("Completed initialization", initTimer);
            _ansiConsole.MarkupLine("");
        }

        private async Task RunWarmupAsync()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            await _ansiConsole.CreateSpectreProgress(TransferSpeedUnit).StartAsync(async ctx =>
            {
                var downloadArguments = new DownloadArguments { MaxConcurrentRequests = (int)MaxConcurrency };
                await _downloadHandler.AttemptDownloadAsync(ctx, LightYellow("Running warmup"), _allRequests, downloadArguments);
            });
        }

        public void PrintSummary(List<Stopwatch> runElapsedTimes, ByteSize totalDownloadSize)
        {
            // White spacing + a horizontal rule to delineate that the command has completed
            _ansiConsole.WriteLine();
            _ansiConsole.Write(new Rule());

            // Building out summary table
            var summaryTable = new Table { Border = TableBorder.MinimalHeavyHead };
            summaryTable.AddColumn(new TableColumn(Cyan("Min")).Centered());
            summaryTable.AddColumn(new TableColumn(LightYellow("Average")).Centered());
            summaryTable.AddColumn(new TableColumn(MediumPurple("Max")).Centered());

            var fastestTime = runElapsedTimes.MinBy(e => e.Elapsed.TotalMilliseconds);
            var slowestTime = runElapsedTimes.MaxBy(e => e.Elapsed.TotalMilliseconds);
            var average = TimeSpan.FromMilliseconds(runElapsedTimes.Sum(e => e.Elapsed.TotalMilliseconds) / runElapsedTimes.Count);

            summaryTable.AddRow(totalDownloadSize.CalculateBitrate(slowestTime), totalDownloadSize.CalculateBitrate(average), totalDownloadSize.CalculateBitrate(fastestTime));

            // Setting up final formatting, to make sure padding and alignment is correct
            var grid = new Grid()
                       .AddColumn(new GridColumn())
                       // Summary Table
                       .AddRow(White(Underline("Benchmark summary")))
                       .AddRow(summaryTable)
                       .AddEmptyRow();

            _ansiConsole.Write(new Padder(grid, new Padding(1, 0)));
            _ansiConsole.WriteLine();
        }
    }
}