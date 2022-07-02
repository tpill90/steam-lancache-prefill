using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DepotDownloader.Exceptions;
using DepotDownloader.Models;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2.CDN;
using Utf8Json;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader.Steam
{
    /// <summary>
    /// This class is primarily responsible for querying the Steam network for available CDN servers,
    /// and managing the current list of available servers.
    /// </summary>
    public class CdnPool
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly Steam3Session _steamSession;

        private readonly string _cachedCdnFilePath = $"{AppConfig.ConfigDir}/cdnServers.json";

        private ConcurrentBag<ServerShim> _availableServerEndpoints = new ConcurrentBag<ServerShim>();
        private int _minimumServerCount = 15;

        // TODO is it possible to write unit tests for this class?
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
        public async Task PopulateAvailableServers()
        {
            LoadCachedCdnsFromDisk();
            if (_availableServerEndpoints.Count >= _minimumServerCount)
            {
                return;
            }
            _steamSession.ThrowIfNotConnected();

            // TODO this can take a really long time.  Around 12 seconds
            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Getting available CDNs", async _ =>
            {
                var retryCount = 0;
                while (_availableServerEndpoints.Count < _minimumServerCount && retryCount < 10)
                {
                    //TODO do I need to readd the logic where it weighs CDN servers by their weight?
                    //TODO need to figure out the logged in user's cell id + get servers from that region first
                    var steamServers = await _steamSession.steamContent.GetServersForSteamPipe();
                    var filteredServers = steamServers.Where(e => e.Protocol == Server.ConnectionProtocol.HTTP)
                                                      .Where(e => e.AllowedAppIds.Length == 0)
                                                      .Select(e => new ServerShim
                                                      {
                                                          Host = e.Host,
                                                          Port = e.Port,
                                                          Protocol = e.Protocol,
                                                          VHost = e.VHost
                                                      })
                                                      .ToList();
                    foreach (var server in filteredServers)
                    {
                        _availableServerEndpoints.Add(server);
                    }

                    // Will wait increasingly longer periods when re-trying
                    retryCount++;
                    await Task.Delay(retryCount * 50);
                }
            });

            if (!_availableServerEndpoints.Any())
            {
                throw new CdnExhaustionException("Unable to get available CDN servers from Steam!.  Try again in a few moments...");
            }
            await File.WriteAllTextAsync(_cachedCdnFilePath, JsonSerializer.ToJsonString(_availableServerEndpoints));
        }

        /// <summary>
        /// Loads the list of CDNs used in a previous run from disk, to skip the slow request to Steam.
        /// Cached CDN list will only be valid for 3 days
        /// </summary>
        private void LoadCachedCdnsFromDisk()
        {
			//TODO it seems like keeping this around for a few days is not likely going to work.  Need to test keeping it around for a few hours, and see how
            // many 502's are thrown
            var lastWriteTime = File.GetLastWriteTime(_cachedCdnFilePath);
            TimeSpan delta = DateTime.Now.Subtract(lastWriteTime);

            // Will only re-use the cache if its less than 3 days old
            if (delta.TotalDays <= 3)
            {
                _availableServerEndpoints = JsonSerializer.Deserialize<ConcurrentBag<ServerShim>>(File.ReadAllText(_cachedCdnFilePath));
            }
        }

        /// <summary>
        /// Attempts to take an available connection from the pool.
        /// Once finished with the connection, it should be returned to the pool using <seealso cref="ReturnConnection"/>
        /// </summary>
        /// <returns>A valid Steam CDN server</returns>
        /// <exception cref="CdnExhaustionException">If no servers are available for use, this exception will be thrown.</exception>
        public ServerShim TakeConnection()
        {
            if (_availableServerEndpoints.IsEmpty)
            {
                throw new CdnExhaustionException("Available Steam CDN servers exhausted!  No more servers available to retry!  Try again in a few minutes");
            }
            _availableServerEndpoints.TryTake(out ServerShim server);
            return server;
        }

        /// <summary>
        /// Returns a connection to the pool of available connections, to be re-used later.
        /// Only valid connections should be returned to the pool.
        /// </summary>
        /// <param name="connection">The connection that will be re-added to the pool.</param>
        public void ReturnConnection(ServerShim connection)
        {
            _availableServerEndpoints.Add(connection);
        }
    }
}
