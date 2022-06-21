using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DepotDownloader.Models;
using DepotDownloader.Steam;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2;
using static DepotDownloader.Utils.SpectreColors;

namespace DepotDownloader.Handlers
{
    //TODO make this not static
    //TODO document
    public static class DepotHandler
    {
        //TODO comment
        public static List<DepotInfo> FilterDepotsToDownload(DownloadArguments downloadArgs, List<DepotInfo> allAvailableDepots, DownloadConfig config)
        {
            var depotSectionsFound = new List<DepotInfo>();

            foreach (var depot in allAvailableDepots)
            {
                var configInfo = depot.ConfigInfo;
                if (configInfo == null)
                {
                    depotSectionsFound.Add(depot);
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
                depotSectionsFound.Add(depot);
            }

            if (!depotSectionsFound.Any())
            {
                throw new ContentDownloaderException($"Couldn't find any depots to download for app {downloadArgs.AppId}");
            }

            AnsiConsole.Console.LogMarkupLine($"Filtered {Yellow(depotSectionsFound.Count)} depots for app {Cyan(downloadArgs.AppId)}");

            return depotSectionsFound;
        }

        //TODO comment
        //TODO crappy name
        public static List<DepotDownloadInfo> GetDepotDownloadInfo(List<DepotInfo> depotsToDownload, Steam3Session steam3, AppInfoShim appInfo)
        {
            var timer = Stopwatch.StartNew();
            
            var depotDownloadInfos = new List<DepotDownloadInfo>();
            foreach (var depotInfo in depotsToDownload)
            {
                var info = GetDepotInfo(depotInfo, appInfo, steam3);
                if (info != null)
                {
                    depotDownloadInfos.Add(info);
                }
            }

            AnsiConsole.Console.LogMarkupLine($"Got depot info for {Yellow(depotsToDownload.Count)} depots".PadRight(55), timer.Elapsed);
            return depotDownloadInfos;
        }

        //TODO finish refactoring
        //TODO document
        public static DepotDownloadInfo GetDepotInfo(DepotInfo depotInfo, AppInfoShim app, Steam3Session steam3)
        {
            ulong manifestId = depotInfo.ManifestId;
            var depotId = depotInfo.DepotId;

            if (!AccountHasAccess(depotId, steam3))
            {
                AnsiConsole.WriteLine($"Depot {depotInfo.DepotId} ({depotInfo.Name}) is not available from this account.");
                return null;
            }
            
            // Finds manifestId for a linked app's depot.  
            // TODO can probably refactor this to just find the child depot from the list of depots we already know 
            if (depotInfo.ManifestId == DownloadConfig.INVALID_MANIFEST_ID)
            {
                manifestId = GetManifestId(depotInfo, app, steam3);
                if (manifestId == DownloadConfig.INVALID_MANIFEST_ID)
                {
                    // TODO handle
                    AnsiConsole.WriteLine("Depot {0} ({1}) missing public subsection or manifest section.", depotId, depotInfo.Name);
                    return null;
                }
            }

            // For depots that are proxied through depotfromapp, we still need to resolve the proxy app id
            var containingAppId = app.AppId;
            if (depotInfo.DepotFromApp != null)
            {
                containingAppId = depotInfo.DepotFromApp.Value;
            }
            return new DepotDownloadInfo(depotId, containingAppId, manifestId, depotInfo.Name);
        }

        // TODO document
        public static ulong GetManifestId(DepotInfo depot, AppInfoShim app, Steam3Session steam3)
        {
            var appId = app.AppId;
            var childDepot = app.Depots.FirstOrDefault(e => e.DepotId == depot.DepotId);
            if (childDepot == null)
            {
                return DownloadConfig.INVALID_MANIFEST_ID;
            }

            // Shared depots can either provide manifests, or leave you relying on their parent app.
            // It seems that with the latter, "sharedinstall" will exist (and equals 2 in the one existance I know of).
            // Rather than relay on the unknown sharedinstall key, just look for manifests. Test cases: 111710, 346680.
            if (childDepot.ManifestId == DownloadConfig.INVALID_MANIFEST_ID && childDepot.DepotFromApp != null)
            {
                var otherAppId = childDepot.DepotFromApp.Value;
                if (otherAppId == appId)
                {
                    //TODO handle
                    // This shouldn't ever happen, but ya never know with Valve. Don't infinite loop.
                    AnsiConsole.WriteLine("App {0}, Depot {1} has depotfromapp of {2}!", appId, depot.DepotId, otherAppId);
                    return DownloadConfig.INVALID_MANIFEST_ID;
                }

                // TODO is this even necessary?
                var otherapp = steam3.RequestAppInfo(otherAppId);

                //TODO wtf recursion
                var returnValue = GetManifestId(depot, otherapp, steam3);
                return returnValue;
            }

            return childDepot.ManifestId;
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
