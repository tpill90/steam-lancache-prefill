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

        private readonly int _minimumServerCount = 5;
        private readonly int _maxRetries = 3;

        public ConcurrentStack<Server> AvailableServerEndpoints = new ConcurrentStack<Server>();

        public CdnPool(IAnsiConsole ansiConsole, Steam3Session steamSession)
        {
            _ansiConsole = ansiConsole;
            _steamSession = steamSession;
        }

        /// <summary>
        /// Constructor used by the benchmark run command in order to avoid logging into Steam to get available CDN servers.
        /// Should not be used other than with the benchmark features.
        /// </summary>
        public CdnPool(IAnsiConsole ansiConsole, ConcurrentStack<Server> availableServers)
        {
            _ansiConsole = ansiConsole;
            AvailableServerEndpoints = availableServers;
        }

        /// <summary>
        /// Gets a list of available CDN servers from the Steam network, will retry until it gets at least 5 available servers.
        /// It is required to manually be called prior to using the class.
        /// </summary>
        /// <exception cref="CdnExhaustionException">If no servers are available for use, this exception will be thrown.</exception>
        public async Task PopulateAvailableServersAsync()
        {
            if (AvailableServerEndpoints.Count >= _minimumServerCount)
            {
                return;
            }

            _ansiConsole.LogMarkupVerbose($"Requesting available CDNs. Pool currently has {LightYellow(AvailableServerEndpoints.Count)} servers available," +
                                          $" below the desired count of {Cyan(_minimumServerCount)}");

            var retryCount = 0;
            var statusMessageBase = White(" Getting available CDN Servers... ");
            await _ansiConsole.StatusSpinner().StartAsync(statusMessageBase, async task =>
            {
                while (AvailableServerEndpoints.Count < _minimumServerCount && retryCount <= _maxRetries)
                {
                    await RequestSteamCdnServersAsync();

                    // Condition prevents the retry message from being displayed on the first run.
                    var retryMessage = retryCount > 0 ? LightYellow($"Retrying {retryCount}") : "";
                    task.Status($"{statusMessageBase} {retryMessage}");
                    await Task.Delay(retryCount * 250);

                    retryCount++;
                }
            });

            if (retryCount == _maxRetries && AvailableServerEndpoints.Empty())
            {
                throw new CdnExhaustionException("Request for Steam CDN servers timed out!");
            }
            if (AvailableServerEndpoints.Empty())
            {
                throw new CdnExhaustionException("Unable to get available CDN servers from Steam!");
            }
        }

        private async Task RequestSteamCdnServersAsync()
        {
            try
            {
                // GetServersForSteamPipe() sometimes hangs and never times out.  Wrapping the call in another task, so that we can timeout the entire method.
                await Task.Run(async () =>
                {
                    var returnedServers = await _steamSession.SteamContent.GetServersForSteamPipe(AppConfig.CellIdOverride);
                    AvailableServerEndpoints.PushRange(returnedServers.ToArray());
                }).WaitAsync(TimeSpan.FromSeconds(15));
            }
            catch (TimeoutException)
            {
                // Swallowing timeout exceptions, so that we can retry and see if the next attempt succeeds
            }

            // Filtering out non-cacheable CDNs.  HTTPS servers are included, as they appear to be able to be manually overridden to HTTP.
            // SteamCache type servers are Valve run.  CDN type servers appear to be ISP run.
            var filteredServers = AvailableServerEndpoints
                                       .Where(e => (e.Type == "SteamCache" || e.Type == "CDN") && e.AllowedAppIds.Length == 0)
                                       .DistinctBy(e => e.Host)
                                       .ToList();

            // TODO Test code
            filteredServers.Last().Host = "cache1-bne-edgx.steamcontent.com";
            filteredServers.Last().VHost = "cache1-bne-edgx.steamcontent.com";

            // Finally checking which servers support HTTPS and adding them back into the list of available servers
            AvailableServerEndpoints.Clear();
            foreach (var server in filteredServers)
            {
                var supportsHttp = await ServerSupportsHttpAsync(server);
                if (supportsHttp)
                {
                    AvailableServerEndpoints.Push(server);
                }
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
            if (AvailableServerEndpoints.Empty())
            {
                throw new CdnExhaustionException("Available Steam CDN servers exhausted!  No more servers available to retry!  Try again in a few minutes");
            }

            AvailableServerEndpoints.TryPop(out var server);
            _ansiConsole.LogMarkupVerbose($"Using CDN {Cyan(server.Host)}");
            return server;
        }

        /// <summary>
        /// Returns a server to the pool of available servers, to be re-used later.
        /// Only valid server should be returned to the pool.
        /// </summary>
        /// <param name="server">The server that will be re-added to the pool.</param>
        public void ReturnConnection(Server server)
        {
            AvailableServerEndpoints.Push(server);
        }

        // TODO comment
        private async Task<bool> ServerSupportsHttpAsync(Server server)
        {
            try
            {
                // This is just an arbitrary file from VC 2005 Redist which has been on Steam's CDNs forever and likely will never go away.
                // https://steamdb.info/depot/228981/manifests/
                var url = $"http://{server.Host}/depot/228981/chunk/652b6c9b4aa15a255b9cd513752dbb82169c9097";
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                // Only the response status matters for the probe, so request a single byte rather than transferring
                // the full ~1MB chunk over a potentially slow connection.  A serving CDN returns 200/206, one that
                // doesn't cache this depot still returns 403.
                requestMessage.Headers.Range = new RangeHeaderValue(0, 0);

                using var cts = new CancellationTokenSource();
                using var client = new HttpClient();
                using var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                // Handle the CDN not supporting HTTP.
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    _ansiConsole.LogMarkupVerbose($"CDN {LightYellow(server.Host)} does not support HTTP.  Skipping");
                    return false;
                }
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                // Transient errors
                return false;
            }

            return true;
        }
    }
}