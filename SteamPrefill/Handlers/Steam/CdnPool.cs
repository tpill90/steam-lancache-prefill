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
        private int _maxRetries = 5;

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
            //TODO need to add a timeout to this GetServersForSteamPipe() call
            if (_availableServerEndpoints.Count >= _minimumServerCount)
            {
                return;
            }

            await _ansiConsole.StatusSpinner().StartAsync(White(" Getting available CDNs "), async task =>
            {
                var retryCount = 0;
                while (_availableServerEndpoints.Count < _minimumServerCount && retryCount < _maxRetries)
                {
                    var returnedServers = await _steamSession.SteamContent.GetServersForSteamPipe();
                    _availableServerEndpoints.AddRange(returnedServers);

                    // Filtering out non-cacheable HTTPs CDNs.  SteamCache type servers are Valve run.  CDN type servers appear to be ISP run.
                    //TODO documentation on why these server types are included?
                    _availableServerEndpoints = _availableServerEndpoints
                                                .Where(e => (e.Type == "SteamCache" || e.Type == "CDN") && e.AllowedAppIds.Length == 0)
                                                .DistinctBy(e => e.Host)
                                                .ToList();

                    task.Status($"{White(" Getting available CDNs ")} {Green($"{_availableServerEndpoints.Count}/{_minimumServerCount}")}");

                    // Will wait increasingly longer periods when re-trying
                    retryCount++;
                    await Task.Delay(retryCount * 250);
                }
            });

#if DEBUG
            PrintDebugInfo();
#endif

            if (_availableServerEndpoints.Empty())
            {
                throw new CdnExhaustionException("Unable to get available CDN servers from Steam!");
            }

            _availableServerEndpoints = _availableServerEndpoints.OrderBy(e => e.WeightedLoad).ToList();
        }

        private void PrintDebugInfo()
        {
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
    }
}