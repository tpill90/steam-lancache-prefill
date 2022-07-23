using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using SteamKit2.CDN;
using SteamPrefill.Models.Exceptions;
using SteamPrefill.Utils;

namespace SteamPrefill.Handlers.Steam
{
    /// <summary>
    /// This class is primarily responsible for querying the Steam network for available CDN servers,
    /// and managing the current list of available servers.
    /// </summary>
    public class CdnPool
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly Steam3Session _steamSession;
        
        private ConcurrentQueue<Server> _availableServerEndpoints = new ConcurrentQueue<Server>();
        private int _minimumServerCount = 5;

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
            await _ansiConsole.StatusSpinner().StartAsync("Getting available CDNs", async _ =>
            {
                var retryCount = 0;
                while (_availableServerEndpoints.Count < _minimumServerCount && retryCount < 10)
                {
                    var allServers = await _steamSession.SteamContent.GetServersForSteamPipe();
                    var filteredServers = allServers.Where(e => e.Type == "SteamCache" && e.AllowedAppIds.Length == 0).ToList();
                    foreach (var server in filteredServers)
                    {
                        _availableServerEndpoints.Enqueue(server);
                    }

                    // Will wait increasingly longer periods when re-trying
                    retryCount++;
                    await Task.Delay(retryCount * 100);
                }
            });

            _availableServerEndpoints = _availableServerEndpoints.OrderBy(e => e.WeightedLoad).ToConcurrentBag();

            if (!_availableServerEndpoints.Any())
            {
                throw new CdnExhaustionException("Unable to get available CDN servers from Steam!.  Try again in a few moments...");
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
            if (_availableServerEndpoints.IsEmpty)
            {
                throw new CdnExhaustionException("Available Steam CDN servers exhausted!  No more servers available to retry!  Try again in a few minutes");
            }
            _availableServerEndpoints.TryDequeue(out Server server);
            return server;
        }

        /// <summary>
        /// Returns a connection to the pool of available connections, to be re-used later.
        /// Only valid connections should be returned to the pool.
        /// </summary>
        /// <param name="connection">The connection that will be re-added to the pool.</param>
        public void ReturnConnection(Server connection)
        {
            _availableServerEndpoints.Enqueue(connection);
        }
    }
}
