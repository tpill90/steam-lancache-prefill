namespace SteamPrefill.Models
{
    public sealed class PrefillSummaryResult
    {
        public int AlreadyUpToDate { get; set; }
        public int FailedApps { get; set; }
        public int Updated { get; set; }
        public int UnownedAppsSkipped { get; set; }

        private Stopwatch PrefillElapsedTime { get; } = Stopwatch.StartNew();

        public void RenderSummaryTable(IAnsiConsole ansiConsole, int availableGamesCount)
        {
            var table = new Table
            {
                Border = TableBorder.MinimalHeavyHead
            };

            var rowFields = new List<string>();

            // Number of updated apps
            table.AddColumn(new TableColumn(Cyan("Updated")).Centered());
            rowFields.Add(Updated.ToString());

            // Apps already up to date
            table.AddColumn(new TableColumn(Green("Up To Date")).Centered());
            rowFields.Add(AlreadyUpToDate.ToString());

            // Failed
            if (FailedApps > 0)
            {
                table.AddColumn(new TableColumn(Red("Failed")).Centered());
                rowFields.Add(FailedApps.ToString());
            }

            // Unowned
            if (UnownedAppsSkipped > 0)
            {
                table.AddColumn(new TableColumn(LightYellow("Unowned")).Centered());
                rowFields.Add(UnownedAppsSkipped.ToString());
            }
            table.AddRow(rowFields.ToArray());

            var grid = new Grid()
                       .AddColumn(new GridColumn())
                       .AddRow($" Prefilled {Magenta(availableGamesCount)} apps in {LightYellow(PrefillElapsedTime.FormatElapsedString())}")
                       .AddRow(table);

            ansiConsole.Write(new Rule());
            ansiConsole.Write(new Padder(grid, new Padding(1, 0)));
        }
    }
}
