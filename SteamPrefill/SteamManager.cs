namespace SteamPrefill
{
    public sealed class SteamManager : IDisposable
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly DownloadArguments _downloadArgs;

        private readonly Steam3Session _steam3;
        private readonly CdnPool _cdnPool;

        private readonly DepotHandler _depotHandler;
        private readonly AppInfoHandler _appInfoHandler;
        private readonly ManifestHandler _manifestHandler;

        private readonly PrefillSummaryResult _prefillSummaryResult = new PrefillSummaryResult();

        public SteamManager(IAnsiConsole ansiConsole, DownloadArguments downloadArgs)
        {
            _ansiConsole = ansiConsole;
            _downloadArgs = downloadArgs;

            _steam3 = new Steam3Session(_ansiConsole);
            _cdnPool = new CdnPool(_ansiConsole, _steam3);
            _appInfoHandler = new AppInfoHandler(_ansiConsole, _steam3, _steam3.LicenseManager);
            _depotHandler = new DepotHandler(_ansiConsole, _steam3, _appInfoHandler, _cdnPool);
            _manifestHandler = new ManifestHandler(ansiConsole, _cdnPool, _steam3);

            AppConfig.VerboseLogs = true;
        }

        #region Startup + Shutdown

        /// <summary>
        /// Logs the user into the Steam network, and retrieves available CDN servers and account licenses.
        ///
        /// Required to be called first before using SteamManager class.
        /// </summary>
        public async Task InitializeAsync()
        {
            var timer = Stopwatch.StartNew();
            _ansiConsole.LogMarkupLine("Starting login!");

            await _steam3.LoginToSteamAsync();
            _steam3.WaitForLicenseCallback();

            _ansiConsole.LogMarkupLine("Steam session initialization complete!", timer);
            // White spacing + a horizontal rule to delineate that initialization has completed
            _ansiConsole.WriteLine();
            _ansiConsole.Write(new Rule());

        }

        public void Shutdown()
        {
            _steam3.Disconnect();
        }

        public void Dispose()
        {
            _steam3.Dispose();
        }

        #endregion

        public async Task WriteStatsAsync(uint depotId, uint appId, string pathToManifests)
        {
            await _cdnPool.PopulateAvailableServersAsync();

            var manifestIds = File.ReadAllText(pathToManifests)
                                  .Split(",")
                                  .Select(e => ulong.Parse(e))
                                  .ToList();

            var allManifestChunks = new List<ChunkData>();
            foreach (var manifestId in manifestIds)
            {
                var manifest = await _manifestHandler.GetSingleManifestAsync(depotId, appId, manifestId);

                List<ChunkData> allChunks = manifest.Files.SelectMany(f => f.Chunks).ToList();
                allManifestChunks.AddRange(allChunks);
            }

            var grouped = allManifestChunks.GroupBy(e => e.ChunkId).ToList();
            long duplicateBytes = grouped.Where(e => e.Count() > 1)
                                         .Sum(e => e.First().CompressedLength);


            var uniqueBytes = grouped.Where(e => e.Count() == 1)
                                     .Sum(e => e.First().CompressedLength);

            _ansiConsole.LogMarkupLine($"Duplicate data {LightYellow(ByteSize.FromBytes(duplicateBytes).ToBinaryString())}");
            _ansiConsole.LogMarkupLine($"Unique data {Magenta(ByteSize.FromBytes(uniqueBytes).ToBinaryString())}");

            Debugger.Break();

        }

    }
}