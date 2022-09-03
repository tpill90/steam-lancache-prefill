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
        private int _minimumServerCount = 10;
        private int _maxRetries = 10;

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

            await _ansiConsole.StatusSpinner().StartAsync("Getting available CDNs", async _ =>
            {
                var retryCount = 0;
                while (_availableServerEndpoints.Count < _minimumServerCount && retryCount < _maxRetries)
                {
                    var allServers = await _steamSession.SteamContent.GetServersForSteamPipe();
                    _availableServerEndpoints.AddRange(allServers);

                    // Filtering out non-cacheable cdns, and duplicate hosts
                    _availableServerEndpoints = _availableServerEndpoints.Where(e => e.Type == "SteamCache" && e.AllowedAppIds.Length == 0)
                                                                         .DistinctBy(e => e.Host)
                                                                         .ToList();

                    // Will wait increasingly longer periods when re-trying
                    retryCount++;
                    await Task.Delay(retryCount * 100);
                }
            });

            if (!_availableServerEndpoints.Any())
            {
                throw new CdnExhaustionException("Unable to get available CDN servers from Steam!");
            }

            _availableServerEndpoints = _availableServerEndpoints.OrderBy(e => e.WeightedLoad).ToList();
        }
        
        /// <summary>
        /// Attempts to take an available connection from the pool.
        /// Once finished with the connection, it should be returned to the pool using <seealso cref="ReturnConnection"/>
        /// </summary>
        /// <returns>A valid Steam CDN server</returns>
        /// <exception cref="CdnExhaustionException">If no servers are available for use, this exception will be thrown.</exception>
        public Server TakeConnection()
        {
            if (!_availableServerEndpoints.Any())
            {
                throw new CdnExhaustionException("Available Steam CDN servers exhausted!  No more servers available to retry!  Try again in a few minutes");
            }
            var server = _availableServerEndpoints.First();
            _availableServerEndpoints.RemoveAt(0);
            return server;
        }

        public IEnumerable<Server> TakeConnections(int connectionCount)
        {
            for (int i = 0; i < connectionCount; i++)
            {
                yield return TakeConnection();
            }
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

        public void ReturnConnections(List<Server> connections)
        {
            foreach (var connection in connections)
            {
                ReturnConnection(connection);
            }
        }
    }
}