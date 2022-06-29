using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2;

namespace DepotDownloader.Handlers
{
    //TODO make this not static
    //TODO document
    public static class DepotHandler
    {
        //TODO comment
        public static List<DepotInfo> FilterDepotsToDownload(DownloadArguments downloadArgs, List<DepotInfo> allAvailableDepots, DownloadConfig config)
        {
            var filteredDepots = new List<DepotInfo>();

            foreach (var depot in allAvailableDepots)
            {
                var configInfo = depot.ConfigInfo;
                if (configInfo == null)
                {
                    filteredDepots.Add(depot);
                    continue;
                }
                
                // Filtering to only specified operating systems
                if (!config.DownloadAllPlatforms && configInfo.OperatingSystemList != null)
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
                if (!config.DownloadAllLanguages && !String.IsNullOrEmpty(configInfo.Language))
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
        public static async Task BuildLinkedDepotInfo(List<DepotInfo> depotsToDownload, Steam3Session steam3, AppInfoShim app)
        {
            foreach (var depotInfo in depotsToDownload)
            {
                var depotId = depotInfo.DepotId;

                if (!AccountHasAccess(depotId, steam3))
                {
                    AnsiConsole.WriteLine($"Depot {depotInfo.DepotId} ({depotInfo.Name}) is not available from this account.");
                    return;
                }

                // Finds manifestId for a linked app's depot.  
                if (depotInfo.ManifestId == null)
                {
                    depotInfo.ManifestId = await GetLinkedAppManifestId(depotInfo, app, steam3);
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
        public async static Task<ulong?> GetLinkedAppManifestId(DepotInfo depot, AppInfoShim app, Steam3Session steam3)
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

            var parentAppInfo = await steam3.GetAppInfo(parentAppId);
            return parentAppInfo.Depots.FirstOrDefault(e => e.DepotId == depot.DepotId).ManifestId;
        }

        // TODO clean this up
        // TODO is this really necessary?
        public static bool AccountHasAccess(uint depotId, Steam3Session steam3)
        {
            if (steam3 == null || steam3.steamUser.SteamID == null || (steam3.Licenses == null && steam3.steamUser.SteamID.AccountType != EAccountType.AnonUser))
            {
                return false;
            }

            List<uint> licenseQuery;
            if (steam3.steamUser.SteamID.AccountType == EAccountType.AnonUser)
            {
                licenseQuery = new List<uint> { 17906 };
            }
            else
            {
                licenseQuery = steam3.Licenses.Select(x => x.PackageID).Distinct().ToList();
            }

            steam3.RequestPackageInfo(licenseQuery);

            foreach (var license in licenseQuery)
            {
                PackageInfoShim package;
                if (steam3.PackageInfoShims.TryGetValue(license, out package) && package != null)
                {
                    if (package.AppIds.Any(e => e == depotId))
                    {
                        return true;
                    }
                    if (package.DepotIds.Any(e => e == depotId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
