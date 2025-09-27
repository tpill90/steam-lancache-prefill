// ReSharper disable MemberCanBePrivate.Global - Properties used as parameters can't be private with CliFx, otherwise they won't work.
namespace SteamPrefill.CliCommands.Benchmark
{
    [UsedImplicitly]
    [Command("benchmark run", Description = "Runs a benchmark using the previously configured workload.")]
    [SuppressMessage("Design", "CA1001:Type owns disposable field(s), but is not disposable", Justification = "Doesn't matter here, as this will die with the app on completion.")]
    public class BenchmarkRunCommand
    {

        private IAnsiConsole _ansiConsole;

        private CdnPool _cdnPool;
        private List<QueuedRequest> _allRequests;


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