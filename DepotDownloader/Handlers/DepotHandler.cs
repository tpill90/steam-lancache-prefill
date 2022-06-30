using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader.Handlers
{
    //TODO document
    public class DepotHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly Steam3Session _steam3Session;

        public DepotHandler(IAnsiConsole ansiConsole, Steam3Session steam3Session)
        {
            _ansiConsole = ansiConsole;
            _steam3Session = steam3Session;
        }

        //TODO comment
        public List<DepotInfo> FilterDepotsToDownload(DownloadArguments downloadArgs, List<DepotInfo> allAvailableDepots, AppConfig config)
        {
            var filteredDepots = new List<DepotInfo>();

            foreach (var depot in allAvailableDepots)
            {
                if (!AccountHasAccess(depot.DepotId))
                {
                    //TODO should this be handled differently? Return a value saying that this was unsuccessful?  
                    _ansiConsole.MarkupLine(White(depot) + Yellow(" is not available from this account."));
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

            if (!filteredDepots.Any())
            {
                throw new ContentDownloaderException($"Couldn't find any depots to download for app {downloadArgs.AppId}");
            }
            return filteredDepots;
        }

        //TODO comment
        public async Task BuildLinkedDepotInfo(List<DepotInfo> depotsToDownload, AppInfoShim app)
        {
            foreach (var depotInfo in depotsToDownload)
            {
                // Finds manifestId for a linked app's depot.  
                if (depotInfo.ManifestId == null)
                {
                    depotInfo.ManifestId = await GetLinkedAppManifestId(depotInfo, app);
                }

                // For depots that are proxied through depotfromapp, we still need to resolve the proxy app id
                depotInfo.ContaingAppId = app.AppId;
                if (depotInfo.DepotFromApp != null)
                {
                    depotInfo.ContaingAppId = depotInfo.DepotFromApp.Value;
                }
            }
        }

        // TODO document
        public async Task<ulong?> GetLinkedAppManifestId(DepotInfo depot, AppInfoShim app)
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

            var parentAppInfo = await _steam3Session.GetAppInfo(parentAppId);
            return parentAppInfo.Depots.FirstOrDefault(e => e.DepotId == depot.DepotId).ManifestId;
        }

        // TODO clean this up
        // TODO document
        // TODO I don't like the name appOrDepotId
        public bool AccountHasAccess(uint appOrDepotId)
        {
            // TODO make it so that if licenses are null, they get loaded automatically.  Without requiring the LoadLicenses() call to be made
            if (_steam3Session.OwnedPackageLicenses == null)
            {
                throw new Exception($"Licenses must be loaded before calling{nameof(AccountHasAccess)}");
            }

            // TODO is there anything to be done with this?
            // https://steamdb.info/sub/17906/apps/
            uint AnonymousDedicatedServerComp = 17906;

            if (_steam3Session.OwnedAppIds.Contains(appOrDepotId))
            {
                return true;
            }
            if (_steam3Session.OwnedDepotIds.Contains(appOrDepotId))
            {
                return true;
            }

            return false;
        }
    }
}
