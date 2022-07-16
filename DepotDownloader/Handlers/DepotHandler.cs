using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Settings;
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using Utf8Json;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader.Handlers
{
    //TODO document
    //TODO finish cleaning up
    public class DepotHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly Steam3Session _steam3Session;
        private readonly AppInfoHandler _appInfoHandler;

        //TODO document
        //TODO should this have a better type?  Kinda gives you no idea what is being stored here
        private readonly Dictionary<uint, List<ulong>> SuccessfullyDownloadedDepots = new Dictionary<uint, List<ulong>>();
        private readonly string _downloadedDepotsPath = $"{AppConfig.ConfigDir}/successfullyDownloadedDepots.json";

        public DepotHandler(IAnsiConsole ansiConsole, Steam3Session steam3Session, AppInfoHandler appInfoHandler)
        {
            _ansiConsole = ansiConsole;
            _steam3Session = steam3Session;
            _appInfoHandler = appInfoHandler;

            //TODO measure performance
            if (File.Exists(_downloadedDepotsPath))
            {
                SuccessfullyDownloadedDepots = JsonSerializer.Deserialize<Dictionary<uint, List<ulong>>>(File.ReadAllText(_downloadedDepotsPath));
            }
        }

        //TODO document
        //TODO measure performance of this on a large set of data
        public void MarkDownloadAsSuccessful(List<DepotInfo> depots)
        {
            foreach (var depot in depots)
            {
                if (!SuccessfullyDownloadedDepots.ContainsKey(depot.DepotId))
                {
                    SuccessfullyDownloadedDepots.Add(depot.DepotId, new List<ulong>());
                }
                var successfulManifests = SuccessfullyDownloadedDepots[depot.DepotId];
                successfulManifests.Add(depot.ManifestId.Value);
            }
            File.WriteAllText(_downloadedDepotsPath, JsonSerializer.ToJsonString(SuccessfullyDownloadedDepots));
        }

        //TODO document
        private bool HasDepotBeenPreviouslyDownloaded(DepotInfo depot)
        {
            if (!SuccessfullyDownloadedDepots.ContainsKey(depot.DepotId))
            {
                return false;
            }
            var successfulManifests = SuccessfullyDownloadedDepots[depot.DepotId];
            return successfulManifests.Contains(depot.ManifestId.Value);
        }

        //TODO document
        //TODO change this to not filter on previously downloaded depots, but rather previously downloaded apps
        public bool AppHasUpdatedDepots(List<DepotInfo> depots)
        {
            return depots.Any(e => HasDepotBeenPreviouslyDownloaded(e) == false);
        }

        //TODO document
        public List<DepotInfo> RemoveInvalidDepots(List<DepotInfo> depots)
        {
            //TODO I don't think this belongs here
            if (depots == null)
            {
                return new List<DepotInfo>();
            }
            var results = new List<DepotInfo>();
            foreach (var depot in depots)
            {
                if (!(depot.ManifestId == null && depot.DepotFromApp == null))
                {
                    results.Add(depot);
                }
                else
                {
                    //TODO reenable, should this be logged to a file or something?
                    //_ansiConsole.MarkupLine("  " + White(depot) + Yellow(" appears to be an invalid depot."));
                }
            }
            return results;
        }
        
        //TODO comment
        public List<DepotInfo> FilterDepotsToDownload(DownloadArguments downloadArgs, List<DepotInfo> allAvailableDepots)
        {
            var filteredDepots = new List<DepotInfo>();

            foreach (var depot in allAvailableDepots)
            {
                if (!_steam3Session.AccountHasDepotAccess(depot.DepotId))
                {
					//TODO cleanup
                    if (depot.ConfigInfo == null)
                    {
                        continue;
                    }
                    //TODO should this be handled differently? Return a value saying that this was unsuccessful?  
                    //if (!depot.Name.Contains("low violence") || (depot.LvCache == null && depot.ConfigInfo.LowViolence == false))
                    //{
                    //    //_ansiConsole.MarkupLine("  " + White(depot) + Yellow(" is not available from this account."));
                    //}
                    continue;
                }

                var configInfo = depot.ConfigInfo;
                if (configInfo == null)
                {
                    filteredDepots.Add(depot);
                    continue;
                }
                
                // Filtering to only specified operating systems
                if (!downloadArgs.DownloadAllPlatforms && configInfo.OperatingSystemList != null)
                {
                    // TODO test this condition
                    if (!configInfo.OperatingSystemList.Contains(downloadArgs.OperatingSystem))
                    {
                        continue;
                    }
                }

                // Architecture
                if (!string.IsNullOrWhiteSpace(configInfo.Architecture))
                {
                    // TODO test this condition
                    if (configInfo.Architecture != (downloadArgs.Architecture ?? Util.GetSteamArch()))
                    {
                        continue;
                    }
                }

                // Language
                if (!downloadArgs.DownloadAllLanguages && !String.IsNullOrEmpty(configInfo.Language))
                {
                    // TODO test this condition
                    if (configInfo.Language != downloadArgs.Language)
                    {
                        continue;
                    }
                }

                // Filter out Low Violence depots, unless specified to be included
                if (!downloadArgs.LowViolence && configInfo.LowViolence)
                {
                    // TODO test this condition
                    continue;
                }
                filteredDepots.Add(depot);
            }

            
            return filteredDepots;
        }

        //TODO comment
        public async Task BuildLinkedDepotInfo(List<DepotInfo> depotsToDownload, AppInfoShim app)
        {
            foreach (var depotInfo in depotsToDownload)
            {
                // Finds manifestId for a linked app.  
                if (depotInfo.ManifestId == null)
                {
                    depotInfo.ManifestId = await GetLinkedAppManifestId(depotInfo, app);
                }

                // For depots that are proxied through depotfromapp, we still need to resolve the proxy app id
                depotInfo.ContainingAppId = app.AppId;

                //TODO Can this be simplified and done when the depot object is being built?
                if (depotInfo.DlcAppId != null)
                {
                    depotInfo.ContainingAppId = depotInfo.DlcAppId.Value;
                }
                if (depotInfo.DepotFromApp != null)
                {
                    depotInfo.ContainingAppId = depotInfo.DepotFromApp.Value;
                }
            }
        }
        
        // TODO document
        private async Task<ulong?> GetLinkedAppManifestId(DepotInfo depot, AppInfoShim app)
        {
            // Shared depots can either provide manifests, or leave you relying on their parent app.
            // It seems that with the latter, "sharedinstall" will exist (and equals 2 in the one existance I know of).
            // Rather than relay on the unknown sharedinstall key, just look for manifests. Test cases: 111710, 346680.
            var parentAppId = depot.DepotFromApp.Value;
            if (parentAppId == app.AppId)
            {
                //TODO handle
                // This shouldn't ever happen, but ya never know with Valve. Don't infinite loop.
                throw new Exception($"App {app.AppId}, Depot {depot.DepotId} has depotfromapp of {parentAppId}!");
            }

            var parentAppInfo = await _appInfoHandler.GetAppInfo(parentAppId);
            return parentAppInfo.Depots.FirstOrDefault(e => e.DepotId == depot.DepotId).ManifestId;
        }
    }
}
