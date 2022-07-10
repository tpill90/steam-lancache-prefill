using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DepotDownloader.Models;
using DepotDownloader.Protos;
using DepotDownloader.Utils;
using Spectre.Console;
using SteamKit2;
using SteamKit2.CDN;
using static DepotDownloader.Utils.SpectreColors;
using JsonSerializer = Utf8Json.JsonSerializer;

namespace DepotDownloader.Steam
{
    public class Steam3Session
    {
        //TODO document
        private List<uint> OwnedPackageLicenses { get; set; }

        // TODO this is all games owned + dlc. Could this possibly be filtered?
        // TODO make this private again
        public HashSet<uint> OwnedAppIds { get; set; } = new HashSet<uint>();
        private HashSet<uint> OwnedDepotIds { get; set; } = new HashSet<uint>();
        
        private readonly SteamClient _steamClient;
        private readonly SteamUser _steamUser;
        public readonly SteamContent steamContent;
        public readonly SteamApps SteamAppsApi;

        //TODO not sure if this should be public or not
        public readonly Client CdnClient;

        private readonly CallbackManager callbacks;
        
        private SteamUser.LogOnDetails logonDetails;
        private readonly IAnsiConsole _ansiConsole;

        // output
        readonly Credentials credentials;

        public delegate bool WaitCondition();

        private readonly object steamLock = new object();
        static readonly TimeSpan STEAM3_TIMEOUT = TimeSpan.FromSeconds(30);
        
        public Steam3Session(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
            
            credentials = new Credentials();
            
            _steamClient = new SteamClient();
            _steamUser = _steamClient.GetHandler<SteamUser>();
            SteamAppsApi = _steamClient.GetHandler<SteamApps>();
            steamContent = _steamClient.GetHandler<SteamContent>();

            callbacks = new CallbackManager(_steamClient);

            callbacks.Subscribe<SteamUser.LoggedOnCallback>(LogOnCallback);
            callbacks.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            callbacks.Subscribe<SteamUser.LoginKeyCallback>(LoginKeyCallback);
            callbacks.Subscribe<SteamClient.ConnectedCallback>(ConnectedCallback);
            callbacks.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);

            callbacks.RunCallbacks();

            CdnClient = new Client(_steamClient);
        }

        #region Connecting to Steam

        private bool _isConnected;
        private bool _connectingToSteamIsRunning;
        DateTime _connectTime;

        public void ConnectToSteam()
        {
            _isConnected = false;
            _connectingToSteamIsRunning = true;
            _connectTime = DateTime.Now;
            
            _steamClient.Connect();

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

        //TODO document
        public void ConfigureLoginDetails(string username, AppConfig Config)
        {
            if (String.IsNullOrEmpty(username))
            {
                //TODO better exception
                throw new Exception("Username cannot be null or empty!");
            }

            string loginKey;
            AccountSettingsStore.Instance.LoginKeys.TryGetValue(username, out loginKey);
            if (loginKey == null)
            {
                // capture the supplied password in case we need to re-use it after checking the login key
                Config.SuppliedPassword = Util.ReadPassword();
            }

            logonDetails = new SteamUser.LogOnDetails
            {
                Username = username,
                Password = loginKey == null ? Config.SuppliedPassword : null,
                ShouldRememberPassword = Config.RememberPassword,
                LoginKey = loginKey,
                LoginID = 0x534DD2
            };
            if (AccountSettingsStore.Instance.SentryData.TryGetValue(logonDetails.Username, out var bytes))
            {
                logonDetails.SentryFileHash = bytes.ToShaHash();
            }
        }

        public void LoginToSteam()
        {
            _ansiConsole.CreateSpectreStatusSpinner().Start("Logging into steam", _ =>
            {
            });

            // TODO re-wrap with a status spinner, and figure how to handle input/output while it is running
            EResult loginResult = EResult.Invalid;
            while (loginResult != EResult.OK)
            {
                _logonResult = null;
                ConnectToSteam();
                _steamUser.LogOn(logonDetails);
                loginResult = WaitForValidCredentials();
            }
            
            TryWaitForLoginKey();
        }

        //TODO this needs thorough testing
        private EResult? _logonResult;
        private void LogOnCallback(SteamUser.LoggedOnCallback loggedOn)
        {
            _logonResult = loggedOn.Result;
            var isSteamGuard = loggedOn.Result == EResult.AccountLogonDenied;
            var is2FA = loggedOn.Result == EResult.AccountLoginDeniedNeedTwoFactor;
            var isLoginKey = SteamManager.Config.RememberPassword && logonDetails.LoginKey != null && loggedOn.Result == EResult.InvalidPassword;

            if (isSteamGuard || is2FA || isLoginKey)
            {
                if (!isLoginKey)
                {
                    _ansiConsole.WriteLine("This account is protected by Steam Guard.");
                }

                if (is2FA)
                {
                    do
                    {
                        _ansiConsole.Write("Please enter your 2 factor auth code from your authenticator app: ");
                        logonDetails.TwoFactorCode = Console.ReadLine();
                    } while (string.Empty == logonDetails.TwoFactorCode);

                    //_steamUser.LogOn(logonDetails);
                    return;
                }
                else if (isLoginKey)
                {
                    AccountSettingsStore.Instance.LoginKeys.Remove(logonDetails.Username);
                    AccountSettingsStore.Save();

                    logonDetails.LoginKey = null;

                    if (SteamManager.Config.SuppliedPassword != null)
                    {
                        // TODO this happening in the middle of a run.  Is there a way to make sure this doesn't happen?
                        _ansiConsole.WriteLine("Login key was expired. Connecting with supplied password.");
                        logonDetails.Password = SteamManager.Config.SuppliedPassword;
                    }
                    else
                    {
                        _ansiConsole.Write("Login key was expired. Please enter your password: ");
                        logonDetails.Password = Util.ReadPassword();
                    }
                }
                else
                {
                    do
                    {
                        _ansiConsole.Write("Please enter the authentication code sent to your email address: ");
                        logonDetails.AuthCode = Console.ReadLine();
                    } while (string.Empty == logonDetails.AuthCode);
                }

                _ansiConsole.Write("Retrying Steam3 connection...");
                ConnectToSteam();

                return;
            }

            if (loggedOn.Result == EResult.TryAnotherCM)
            {
                _ansiConsole.Write("Retrying Steam3 connection (TryAnotherCM)...");

                Reconnect();

                return;
            }

            if (loggedOn.Result == EResult.ServiceUnavailable)
            {
                Console.WriteLine("Unable to login to Steam3: {0}", loggedOn.Result);
                throw new Exception("aborted");
            }

            if (loggedOn.Result != EResult.OK)
            {
                Console.WriteLine("Unable to login to Steam3: {0}", loggedOn.Result);
                throw new Exception("aborted");
            }

            credentials.LoggedOn = true;

            _ansiConsole.LogMarkupLine($"Logged '{Cyan(logonDetails.Username)}' into Steam3...");
          
            //TODO test this
            AppConfig.CellID = (int)loggedOn.CellID;
        }

        //TODO document
        public EResult WaitForValidCredentials()
        {
            while (_logonResult == null)
            {
                callbacks.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                if (DateTime.Now - _connectTime > STEAM3_TIMEOUT && !_isConnected)
                {
                    _ansiConsole.WriteLine("Timeout connecting to Steam3.");
                    throw new Exception("aborted");
                }
            }
            return _logonResult.Value;
            if (!credentials.IsValid)
            {
                //TODO better exception type
                //TODO is this even possible?
                _ansiConsole.MarkupLine($"{Red("Error: Login to Steam failed")}");
                throw new Exception("Unable to get steam3 credentials.");
            }
        }

        bool _receivedLoginKey;
        //TODO cleanup + comment
        public void TryWaitForLoginKey()
        {
            if (logonDetails.LoginKey != null)
            {
                return;
            }

            var totalWaitPeriod = DateTime.Now.AddSeconds(3);
            while (true)
            {
                if (DateTime.Now >= totalWaitPeriod)
                {
                    return;
                }
                if (_receivedLoginKey)
                {
                    return;
                }
                callbacks.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(100));
            }
        }

        private void LoginKeyCallback(SteamUser.LoginKeyCallback loginKey)
        {
            AccountSettingsStore.Instance.LoginKeys[logonDetails.Username] = loginKey.LoginKey;
            AccountSettingsStore.Save();

            _steamUser.AcceptNewLoginKey(loginKey);
            _receivedLoginKey = true;
        }

        public void ThrowIfNotConnected()
        {
            //TODO should probably handle this better than just throwing
            if (!_steamClient.IsConnected)
            {
                //TODO better exception type and message
                throw new Exception("Steam session not connected");
            }
        }

        #endregion

        #region Other Auth Methods

        private void Reconnect()
        {
            _steamClient.Disconnect();
        }

        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth)
        {
            var hash = Util.ToShaHash(machineAuth.Data);
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
            _steamUser.SendMachineAuthResponse(authResponse);
        }

        

        #endregion

        #region LoadAccountLicenses

        private bool _loadAccountLicensesIsRunning = true;

        public void LoadAccountLicenses()
        {
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
                //TODO handle
                _ansiConsole.WriteLine($"Unable to get license list: {licenseList.Result}");
                throw new Exception("aborted");

                return;
            }

            OwnedPackageLicenses = licenseList.LicenseList.Select(x => x.PackageID)
                                              .Distinct()
                                              .ToList();

            LoadPackageInfo(OwnedPackageLicenses);
        }

        //TODO document
        private void LoadPackageInfo(List<uint> packageIds)
        {
            // TODO consider turning this into a class, for the sake of readability
            var packageCountPath = $"{AppConfig.ConfigDir}/packageCount.txt";
            var ownedAppIdsPath = $"{AppConfig.ConfigDir}/OwnedAppIds.json";
            var ownedDepotIdsPath = $"{AppConfig.ConfigDir}/OwnedDepotIds.json";

            // If we haven't bought any new games (or free-to-play) since the last run, we can reload our owned Apps/Depots
            if (File.Exists(packageCountPath) && File.ReadAllText(packageCountPath) == packageIds.Count.ToString())
            {
                if (File.Exists(ownedAppIdsPath) && File.Exists(ownedDepotIdsPath))
                {
                    OwnedAppIds = JsonSerializer.Deserialize<HashSet<uint>>(File.ReadAllText($"{AppConfig.ConfigDir}/OwnedAppIds.json"));
                    OwnedDepotIds = JsonSerializer.Deserialize<HashSet<uint>>(File.ReadAllText($"{AppConfig.ConfigDir}/OwnedDepotIds.json"));
                    return;
                }
            }
            
            var packageRequests = packageIds.Select(package => new SteamApps.PICSRequest(package)).ToList();
            //TODO async
            var jobResult = SteamAppsApi.PICSGetProductInfo(new List<SteamApps.PICSRequest>(), packageRequests).ToTask().Result;
            if (!jobResult.Complete)
            {
                //TODO not sure this ever happens
                throw new Exception("Job not complete");
            }

            var packages = jobResult.Results.SelectMany(e => e.Packages)
                                    .Select(e => e.Value)
                                    .ToList();
            foreach (var package in packages)
            {
                // Removing any free weekends that are no longer active
                //TODO turn this into a property on some class
                var expiryTimeKey = package.KeyValues["extended"].Children.FirstOrDefault(e => e.Name == "expirytime");
                if (expiryTimeKey != null)
                {
                    var expiryTimeUtc = DateTimeOffset.FromUnixTimeSeconds(1630256400).DateTime;
                    if (DateTime.Now > expiryTimeUtc)
                    {
                        continue;
                    }
                }
                foreach (KeyValue appId in package.KeyValues["appids"].Children)
                {
                    OwnedAppIds.Add(UInt32.Parse(appId.Value));
                }
                foreach (KeyValue appId in package.KeyValues["depotids"].Children)
                {
                    OwnedDepotIds.Add(UInt32.Parse(appId.Value));
                }
            }

            File.WriteAllText(ownedAppIdsPath, JsonSerializer.ToJsonString(OwnedAppIds));
            File.WriteAllText(ownedDepotIdsPath, JsonSerializer.ToJsonString(OwnedDepotIds));
            File.WriteAllText(packageCountPath, packageIds.Count.ToString());
        }
        #endregion

        //TODO move this into app handler?
        /// <summary>
        /// Checks against the list of currently owned apps to determine if the user is able to download this app.
        /// </summary>
        /// <param name="appid">Id of the application to check for access</param>
        /// <returns>True if the user has access to the app</returns>
        public bool AccountHasAppAccess(uint appid)
        {
            return OwnedAppIds.Contains(appid);
        }

        //TODO move this into depot handler?
        /// <summary>
        /// Checks against the list of currently owned apps to determine if the user is able to download this depot.
        /// </summary>
        /// <param name="depotId">Id of the depot to check for access</param>
        /// <returns>True if the user has access to the depot</returns>
        public bool AccountHasDepotAccess(uint depotId)
        {
            return OwnedDepotIds.Contains(depotId);
        }
    }
}
