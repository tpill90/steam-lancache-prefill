using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2.CDN;

namespace DepotDownloader.Steam
{
    // TODO Document class
    public class CDNClientPool
    {
        private readonly Steam3Session _steamSession;

        public Client CDNClient { get; }

        private List<ServerShim> _availableServerEndpoints = new List<ServerShim>();
        
        public CDNClientPool(Steam3Session steamSession)
        {
            _steamSession = steamSession;
            CDNClient = new Client(steamSession.steamClient);
        }
        
        //TODO document that this is required to be called
        //TODO can probably load this from disk.  I wonder how frequently they change.  Maybe check once per hour?
        //TODO handle no servers being returned
        public async Task PopulateAvailableServers()
        {
            var timer = Stopwatch.StartNew();

            var cacheFile = $"{DownloadConfig.ConfigDir}/cdnServers.json";
            //TODO only use this cache file for a few hours.  CDN servers might have changed
            if (File.Exists(cacheFile))
            {
                _availableServerEndpoints = Utf8Json.JsonSerializer.Deserialize<List<ServerShim>>(File.ReadAllText(cacheFile));
                if (_availableServerEndpoints.Count != 0)
                {
                    AnsiConsole.Console.LogMarkupLine("Populated available Steam CDN servers", timer.Elapsed);
                    return;
                }
            }

            if (!_steamSession.steamClient.IsConnected)
            {
                //TODO better exception type and message
                throw new Exception("Steam session not connected");
            }
            
            var servers = await _steamSession.steamContent.GetServersForSteamPipe();
            if (servers == null || servers.Count == 0)
            {
                //TODO better exception type and message
                throw new Exception("No CDN servers found!");
            }

            _availableServerEndpoints = servers.Where(e => e.Protocol == Server.ConnectionProtocol.HTTP)
                                               .Where(e => e.AllowedAppIds.Length == 0)
                                               .OrderBy(server => server.WeightedLoad)
                                               .Select(e => new ServerShim
                                               {
                                                   Host = e.Host,
                                                   CellID = e.CellID,
                                                   Port = e.Port,
                                                   Protocol = e.Protocol,
                                                   VHost = e.VHost
                                               })
                                               .ToList();

            File.WriteAllText($"{DownloadConfig.ConfigDir}/cdnServers.json", Utf8Json.JsonSerializer.ToJsonString(_availableServerEndpoints));
            AnsiConsole.Console.LogMarkupLine("Populated available Steam CDN servers", timer.Elapsed);
        }

        //TODO possibly cycle these in the case of failures, cycle every time there is a retry
        public ServerShim GetConnection()
        {
            //TODO throw exception if there is no server
            return _availableServerEndpoints.FirstOrDefault();
        }
    }
}
