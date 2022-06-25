using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DepotDownloader.Models;
using DepotDownloader.Protos;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2;
using static DepotDownloader.Utils.SpectreColors;
using JsonSerializer = Utf8Json.JsonSerializer;

namespace DepotDownloader.Steam
{
    public class Steam3Session
    {
        public ReadOnlyCollection<SteamApps.LicenseListCallback.License> Licenses { get; private set; }

        public Dictionary<uint, ulong> AppTokens { get; private set; }
        public Dictionary<uint, ulong> PackageTokens { get; private set; }
        public Dictionary<uint, byte[]> DepotKeys { get; private set; }
        
        public Dictionary<uint, AppInfoShim> AppInfoShims { get; private set; } = new Dictionary<uint, AppInfoShim>();
        public Dictionary<uint, PackageInfoShim> PackageInfoShims { get; private set; } = new Dictionary<uint, PackageInfoShim>();

        public SteamClient steamClient;
        public SteamUser steamUser;
        public SteamContent steamContent;
        readonly SteamApps steamApps;

        readonly CallbackManager callbacks;
        
        readonly bool authenticatedUser;
        bool bAborted;
        int seq; // more hack fixes
        
        // input
        readonly SteamUser.LogOnDetails logonDetails;
        private readonly IAnsiConsole _ansiConsole;

        // output
        readonly Credentials credentials;

        public delegate bool WaitCondition();

        private readonly object steamLock = new object();


        static readonly TimeSpan STEAM3_TIMEOUT = TimeSpan.FromSeconds(30);
        
        public Steam3Session(SteamUser.LogOnDetails details, IAnsiConsole ansiConsole)
        {
            logonDetails = details;
            _ansiConsole = ansiConsole;

            authenticatedUser = details.Username != null;
            credentials = new Credentials();
            bAborted = false;
            seq = 0;

            AppTokens = new Dictionary<uint, ulong>();
            PackageTokens = new Dictionary<uint, ulong>();
            DepotKeys = new Dictionary<uint, byte[]>();
            
            steamClient = new SteamClient();

            steamUser = steamClient.GetHandler<SteamUser>();
            steamApps = steamClient.GetHandler<SteamApps>();
            steamContent = steamClient.GetHandler<SteamContent>();

            callbacks = new CallbackManager(steamClient);

            callbacks.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);

            callbacks.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            callbacks.Subscribe<SteamUser.LoginKeyCallback>(LoginKeyCallback);

            callbacks.RunCallbacks();

            if (authenticatedUser)
            {
                var fi = new FileInfo(string.Format("{0}.sentryFile", logonDetails.Username));
                if (AccountSettingsStore.Instance.SentryData != null && AccountSettingsStore.Instance.SentryData.ContainsKey(logonDetails.Username))
                {
                    logonDetails.SentryFileHash = Util.SHAHash(AccountSettingsStore.Instance.SentryData[logonDetails.Username]);
                }
                else if (fi.Exists && fi.Length > 0)
                {
                    var sentryData = File.ReadAllBytes(fi.FullName);
                    logonDetails.SentryFileHash = Util.SHAHash(sentryData);
                    AccountSettingsStore.Instance.SentryData[logonDetails.Username] = sentryData;
                    AccountSettingsStore.Save();
                }
            }
        }

        #region Connecting to Steam

        private bool _isConnected;
        private bool _connectingToSteamIsRunning;
        DateTime _connectTime;

        public void ConnectToSteam()
        {
            bAborted = false;
            _isConnected = false;
            _connectingToSteamIsRunning = true;
            _connectTime = DateTime.Now;
            
            callbacks.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            steamClient.Connect();

            while (_connectingToSteamIsRunning)
            {
                callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        private void ConnectedCallback(SteamClient.ConnectedCallback connected)
        {
            _connectingToSteamIsRunning = false;
            _isConnected = true;
        }

        #endregion

        #region Logging into Steam

        public void LoginToSteam()
        {
            if (!authenticatedUser)
            {
                steamUser.LogOnAnonymous();
            }
            else
            {
                steamUser.LogOn(logonDetails);
            }
        }

        private void LogOnCallback(SteamUser.LoggedOnCallback loggedOn)
        {
            var isSteamGuard = loggedOn.Result == EResult.AccountLogonDenied;
            var is2FA = loggedOn.Result == EResult.AccountLoginDeniedNeedTwoFactor;
            var isLoginKey = SteamManager.Config.RememberPassword && logonDetails.LoginKey != null && loggedOn.Result == EResult.InvalidPassword;

            if (isSteamGuard || is2FA || isLoginKey)
            {
                if (!isLoginKey)
                {
                    Console.WriteLine("This account is protected by Steam Guard.");
                }

                if (is2FA)
                {
                    do
                    {
                        Console.Write("Please enter your 2 factor auth code from your authenticator app: ");
                        logonDetails.TwoFactorCode = Console.ReadLine();
                    } while (string.Empty == logonDetails.TwoFactorCode);
                }
                else if (isLoginKey)
                {
                    AccountSettingsStore.Instance.LoginKeys.Remove(logonDetails.Username);
                    AccountSettingsStore.Save();

                    logonDetails.LoginKey = null;

                    if (SteamManager.Config.SuppliedPassword != null)
                    {
                        Console.WriteLine("Login key was expired. Connecting with supplied password.");
                        logonDetails.Password = SteamManager.Config.SuppliedPassword;
                    }
                    else
                    {
                        Console.Write("Login key was expired. Please enter your password: ");
                        logonDetails.Password = Util.ReadPassword();
                    }
                }
                else
                {
                    do
                    {
                        Console.Write("Please enter the authentication code sent to your email address: ");
                        logonDetails.AuthCode = Console.ReadLine();
                    } while (string.Empty == logonDetails.AuthCode);
                }

                Console.Write("Retrying Steam3 connection...");
                ConnectToSteam();

                return;
            }

            if (loggedOn.Result == EResult.TryAnotherCM)
            {
                Console.Write("Retrying Steam3 connection (TryAnotherCM)...");

                Reconnect();

                return;
            }

            if (loggedOn.Result == EResult.ServiceUnavailable)
            {
                Console.WriteLine("Unable to login to Steam3: {0}", loggedOn.Result);
                Abort();

                return;
            }

            if (loggedOn.Result != EResult.OK)
            {
                Console.WriteLine("Unable to login to Steam3: {0}", loggedOn.Result);
                Abort();

                return;
            }


            seq++;
            credentials.LoggedOn = true;

            SteamManager.Config.CellID = (int)loggedOn.CellID;

            if (!authenticatedUser)
            { 
                _ansiConsole.LogMarkupLine("Logged anonymously into Steam3...");
            }
            else
            {
                _ansiConsole.LogMarkupLine($"Logged '{Cyan(logonDetails.Username)}' into Steam3...");
            }
        }

        #endregion

        //TODO measure performance
        //TODO handle files not existing
        public void LoadCachedData()
        {
            var timer = Stopwatch.StartNew();

            //if (File.Exists($"{DownloadConfig.ConfigDir}/appInfo.json"))
            //{
            //    AppInfoShims = JsonSerializer.Deserialize<Dictionary<uint, AppInfoShim>>(File.ReadAllText($"{DownloadConfig.ConfigDir}/appInfo.json"));
            //}
            if (File.Exists($"{DownloadConfig.ConfigDir}/appTokens.json"))
            {
                AppTokens = JsonSerializer.Deserialize<Dictionary<uint, ulong>>(File.ReadAllText($"{DownloadConfig.ConfigDir}/appTokens.json"));
            }
            if (File.Exists($"{DownloadConfig.ConfigDir}/packageInfo.json"))
            {
                PackageInfoShims = JsonSerializer.Deserialize<Dictionary<uint, PackageInfoShim>>(File.ReadAllText($"{DownloadConfig.ConfigDir}/packageInfo.json"));
            }

            _ansiConsole.LogMarkupLine("Loaded data from disk", timer.Elapsed);
        }

        //TODO test this with very large data sets
        //TODO measure performance
        public void SerializeCachedData()
        {
            var timer = Stopwatch.StartNew();

            //TODO not sure this is a good idea
            //File.WriteAllText($"{DownloadConfig.ConfigDir}/appInfo.json", JsonSerializer.ToJsonString(AppInfoShims));
            File.WriteAllText($"{DownloadConfig.ConfigDir}/appTokens.json", JsonSerializer.ToJsonString(AppTokens));
            File.WriteAllText($"{DownloadConfig.ConfigDir}/packageInfo.json", JsonSerializer.ToJsonString(PackageInfoShims));

            _ansiConsole.WriteLine();
            _ansiConsole.LogMarkupLine("Saved data to disk", timer.Elapsed);
        }
        
        #region LoadAccountLicenses

        private bool _loadAccountLicensesIsRunning = true;
        private Stopwatch _loadAccountLicensesTimer;

        //TODO speed this up by serializing/deserializing?
        public void LoadAccountLicenses()
        {
            _loadAccountLicensesTimer = Stopwatch.StartNew();

            callbacks.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
            while (_loadAccountLicensesIsRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        private void LicenseListCallback(SteamApps.LicenseListCallback licenseList)
        {
            _loadAccountLicensesIsRunning = false;
            if (licenseList.Result != EResult.OK)
            {
                Console.WriteLine("Unable to get license list: {0} ", licenseList.Result);
                Abort();

                return;
            }

            Licenses = licenseList.LicenseList;

            foreach (var license in licenseList.LicenseList)
            {
                if (license.AccessToken > 0)
                {
                    PackageTokens.TryAdd(license.PackageID, license.AccessToken);
                }
            }

            AnsiConsole.Console.LogMarkupLine($"Got {Cyan(licenseList.LicenseList.Count)} licenses for account!".PadRight(54), _loadAccountLicensesTimer.Elapsed);
            _loadAccountLicensesTimer.Stop();
        }
        #endregion

        //TODO fully implement this.  Seems to be faster than an individual load
        public void BulkLoadAppInfos()
        {
            var timer = Stopwatch.StartNew();
            var requests = new List<SteamApps.PICSRequest>();
            foreach (var id in new List<uint>() { 730, 570, 1085660, 440 })
            {
                requests.Add(new SteamApps.PICSRequest(id));
            }

            //TODO async await
            var productJob = steamApps.PICSGetProductInfo(requests, new List<SteamApps.PICSRequest>());
            AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet resultSet = productJob.GetAwaiter().GetResult();
            timer.Stop();
            Debugger.Break();
        }

        //TODO comment
        public AppInfoShim GetAppInfo(uint appId)
        {
            var timer = Stopwatch.StartNew();
            if (AppInfoShims.ContainsKey(appId))
            {
                return AppInfoShims[appId];
            }
            
            var request = new SteamApps.PICSRequest(appId);
            
            //TODO async await
            var productJob = steamApps.PICSGetProductInfo(new List<SteamApps.PICSRequest> { request }, new List<SteamApps.PICSRequest>());
            AsyncJobMultiple<SteamApps.PICSProductInfoCallback>.ResultSet resultSet = productJob.GetAwaiter().GetResult();

            if (resultSet.Complete)
            {
                foreach (var appInfo in resultSet.Results)
                {
                    foreach (var app_value in appInfo.Apps)
                    {
                        var app = app_value.Value;

                        AppInfoShims[app.ID] = new AppInfoShim(app.ID, app.ChangeNumber, app.KeyValues);
                    }

                    foreach (var app in appInfo.UnknownApps)
                    {
                        AppInfoShims[app] = null;
                    }
                }
            }
            _ansiConsole.LogMarkupLine("GetAppInfo complete", timer.Elapsed);
            return AppInfoShims[appId];
        }
        
        public void RequestPackageInfo(List<uint> packages)
        {
            if (PackageInfoShims.Count > 0)
            {
                // Skipping since this is already loaded
                return;
            }

            if (packages.Count == 0 || bAborted)
            {
                return;
            }
            
            //TODO need to serialize this data too
            var packageRequests = new List<SteamApps.PICSRequest>();

            foreach (var package in packages)
            {
                var request = new SteamApps.PICSRequest(package);

                if (PackageTokens.TryGetValue(package, out var token))
                {
                    request.AccessToken = token;
                }

                packageRequests.Add(request);
            }
            var jobResult = steamApps.PICSGetProductInfo(new List<SteamApps.PICSRequest>(), packageRequests).GetAwaiter().GetResult();
            if (jobResult.Complete)
            {
                foreach (var packageInfo in jobResult.Results)
                {
                    foreach (var package_value in packageInfo.Packages)
                    {
                        var package = package_value.Value;

                        var packageInfoShim = new PackageInfoShim
                        {
                            AppIds = package.KeyValues["appids"].Children.Select(e => UInt32.Parse(e.Value)).ToList(),
                            DepotIds = package.KeyValues["depotids"].Children.Select(e => UInt32.Parse(e.Value)).ToList()
                        };

                        PackageInfoShims[package.ID] = packageInfoShim;
                    }

                    foreach (var package in packageInfo.UnknownPackages)
                    {
                        PackageInfoShims[package] = null;
                    }
                }
            }
        }

        //TODO cleanup
        public void RequestDepotKey(uint depotId, uint appid = 0)
        {
            if (DepotKeys.ContainsKey(depotId) || bAborted)
                return;

            var completed = false;

            Action<SteamApps.DepotKeyCallback> cbMethod = depotKey =>
            {
                completed = true;

                if (depotKey.Result != EResult.OK)
                {
                    Abort();
                    return;
                }

                DepotKeys[depotKey.DepotID] = depotKey.DepotKey;
            };

            WaitUntilCallback(() =>
            {
                callbacks.Subscribe(steamApps.GetDepotDecryptionKey(depotId, appid), cbMethod);
            }, () => { return completed; });
        }

        public async Task<ulong> GetDepotManifestRequestCodeAsync(uint depotId, uint appId, ulong manifestId)
        {
            if (bAborted)
            {
                return 0;
            }

            var requestCode = await steamContent.GetManifestRequestCode(depotId, appId, manifestId, "public");
            return requestCode;
        }

        private void Abort()
        {
            throw new Exception("aborted");
        }
        
        private void Reconnect()
        {
            steamClient.Disconnect();
        }

        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth)
        {
            var hash = Util.SHAHash(machineAuth.Data);
            Console.WriteLine("Got Machine Auth: {0} {1} {2} {3}", machineAuth.FileName, machineAuth.Offset, machineAuth.BytesToWrite, machineAuth.Data.Length, hash);

            AccountSettingsStore.Instance.SentryData[logonDetails.Username] = machineAuth.Data;
            AccountSettingsStore.Save();

            var authResponse = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,

                SentryFileHash = hash, // should be the sha1 hash of the sentry file we just wrote

                OneTimePassword = machineAuth.OneTimePassword, // not sure on this one yet, since we've had no examples of steam using OTPs

                LastError = 0, // result from win32 GetLastError
                Result = EResult.OK, // if everything went okay, otherwise ~who knows~

                JobID = machineAuth.JobID, // so we respond to the correct server job
            };

            // send off our response
            steamUser.SendMachineAuthResponse(authResponse);
        }

        private void LoginKeyCallback(SteamUser.LoginKeyCallback loginKey)
        {
            Console.WriteLine("Accepted new login key for account {0}", logonDetails.Username);

            AccountSettingsStore.Instance.LoginKeys[logonDetails.Username] = loginKey.LoginKey;
            AccountSettingsStore.Save();

            steamUser.AcceptNewLoginKey(loginKey);
        }

        #region Callback Waiting methods

        private void WaitForCallbacks()
        {
            callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));

            var diff = DateTime.Now - _connectTime;

            if (diff > STEAM3_TIMEOUT && !_isConnected)
            {
                Console.WriteLine("Timeout connecting to Steam3.");
                Abort();
            }
        }

        public void WaitUntilCallback(Action submitter, WaitCondition waiter)
        {
            while (!bAborted && !waiter())
            {
                lock (steamLock)
                {
                    submitter();
                }

                var seq = this.seq;
                do
                {
                    lock (steamLock)
                    {
                        WaitForCallbacks();
                    }
                } while (!bAborted && this.seq == seq && !waiter());
            }
        }

        public Credentials WaitForCredentials()
        {
            if (credentials.IsValid || bAborted)
                return credentials;

            WaitUntilCallback(() => { }, () => { return credentials.IsValid; });

            return credentials;
        }

        #endregion
    }
}
