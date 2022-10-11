namespace SteamPrefill.Handlers.Steam
{
    /// <summary>
    /// This class is primarily responsible for querying the Steam network for available CDN servers,
    /// and managing the current list of available servers.
    /// </summary>
    public sealed class CdnPool
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly Steam3Session _steamSession;

        private List<Server> _availableServerEndpoints = new List<Server>();

        private int _minimumServerCount = 5;
        private int _maxRetries = 3;

        public CdnPool(IAnsiConsole ansiConsole, Steam3Session steamSession)
        {
            _ansiConsole = ansiConsole;
            _steamSession = steamSession;
        }

        /// <summary>
        /// Gets a list of available CDN servers from the Steam network.
        /// Required to be called prior to using the class.
        /// </summary>
        /// <exception cref="CdnExhaustionException">If no servers are available for use, this exception will be thrown.</exception>
        public async Task PopulateAvailableServersAsync()
        {
            if (_availableServerEndpoints.Count >= _minimumServerCount)
            {
                return;
            }

            var retryCount = 0;
            var statusMessageBase = White(" Getting available CDN Servers... ");
            await _ansiConsole.StatusSpinner().StartAsync(statusMessageBase, async task =>
            {
                while (_availableServerEndpoints.Count < _minimumServerCount && retryCount < _maxRetries)
                {
                    await RequestSteamCdnServersAsync();

                    retryCount++;
                    task.Status($"{statusMessageBase} {LightYellow($"Retrying {retryCount}")}");
                    await Task.Delay(retryCount * 250);
                }
            });

#if DEBUG
            PrintDebugInfo();
#endif

            if (retryCount == _maxRetries && _availableServerEndpoints.Empty())
            {
                throw new CdnExhaustionException("Request for Steam CDN servers timed out!");
            }
            if (_availableServerEndpoints.Empty())
            {
                throw new CdnExhaustionException("Unable to get available CDN servers from Steam!");
            }

            _availableServerEndpoints = _availableServerEndpoints.OrderBy(e => e.WeightedLoad).ToList();
        }

        private async Task RequestSteamCdnServersAsync()
        {
            try
            {
                // GetServersForSteamPipe() sometimes hangs and never times out.  Wrapping the call in another task, so that we can timeout the entire method.
                await Task.Run(async () =>
                {
                    var returnedServers = await _steamSession.SteamContent.GetServersForSteamPipe();
                    _availableServerEndpoints.AddRange(returnedServers);

                    // Filtering out non-cacheable HTTPs CDNs.  SteamCache type servers are Valve run.  CDN type servers appear to be ISP run.
                    _availableServerEndpoints = _availableServerEndpoints
                                                .Where(e => (e.Type == "SteamCache" || e.Type == "CDN") && e.AllowedAppIds.Length == 0)
                                                .DistinctBy(e => e.Host)
                                                .ToList();
                }).WaitAsync(TimeSpan.FromSeconds(6));
            }
            catch (TimeoutException)
            {
                // Swallowing timeout exceptions, so that we can retry and see if the next attempt succeeds
            }
        }

        /// <summary>
        /// Attempts to take an available connection from the pool.
        /// Once finished with the connection, it should be returned to the pool using <seealso cref="ReturnConnection"/>
        /// </summary>
        /// <returns>A valid Steam CDN server</returns>
        /// <exception cref="CdnExhaustionException">If no servers are available for use, this exception will be thrown.</exception>
        public Server TakeConnection()
        {
            if (_availableServerEndpoints.Empty())
            {
                throw new CdnExhaustionException("Available Steam CDN servers exhausted!  No more servers available to retry!  Try again in a few minutes");
            }

            var server = _availableServerEndpoints.First();
            _availableServerEndpoints.RemoveAt(0);
            return server;
        }

        /// <summary>
        /// Returns a connection to the pool of available connections, to be re-used later.
        /// Only valid connections should be returned to the pool.
        /// </summary>
        /// <param name="connection">The connection that will be re-added to the pool.</param>
        public void ReturnConnection(Server connection)
        {
            _availableServerEndpoints.Add(connection);
            _availableServerEndpoints = _availableServerEndpoints.OrderBy(e => e.WeightedLoad).ToList();
        }

        private void PrintDebugInfo()
        {
            if (!AppConfig.VerboseLogs)
            {
                return;
            }

            // Prints out retrieved CDNs
            var table = new Table().AddColumns("Total Results", "_availableServerEndpoints");
            _ansiConsole.Live(table).Start(task =>
            {
                Grid serverGrid = new Grid().AddColumn();
                foreach (Server s in _availableServerEndpoints)
                {
                    serverGrid.AddRow($"{s.Type} {MediumPurple(s.Host)}".ToMarkup());
                    task.Refresh();
                }

                table.AddRow(_availableServerEndpoints.Count.ToMarkup(), serverGrid);
            });
        }
    }
}