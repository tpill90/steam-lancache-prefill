using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private readonly CallbackManager _callbackManager;
        
        private SteamUser.LogOnDetails _logonDetails;
        private readonly IAnsiConsole _ansiConsole;

        public Steam3Session(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
            
            _steamClient = new SteamClient();
            _steamUser = _steamClient.GetHandler<SteamUser>();
            SteamAppsApi = _steamClient.GetHandler<SteamApps>();
            steamContent = _steamClient.GetHandler<SteamContent>();

            _callbackManager = new CallbackManager(_steamClient);

            _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(loggedOn => _loggedOnCallbackResult = loggedOn);
            _callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(UpdateMachineAuthCallback);
            _callbackManager.Subscribe<SteamUser.LoginKeyCallback>(LoginKeyCallback);
            _callbackManager.Subscribe<SteamApps.LicenseListCallback>(LicenseListCallback);
            _callbackManager.Subscribe<SteamClient.ConnectedCallback>(connected => { });

            _callbackManager.RunCallbacks();

            CdnClient = new Client(_steamClient);
        }

        // TODO re-wrap with a status spinner, and figure how to handle input/output while it is running
        // TODO document
        public void LoginToSteam(string username)
        {
            ConfigureLoginDetails(username);

            //TODO I don't like how this is written
            //TODO add in a limited # of retries
            while (true)
            {
                SteamUser.LoggedOnCallback logonResult = null;
                _ansiConsole.CreateSpectreStatusSpinner().Start("Connecting to Steam...", ctx =>
                {
                    ConnectToSteam();

                    ctx.Status = "Logging into Steam...";
                    logonResult = AttemptSteamLogin();
                });

                if (HandleLogonResult(logonResult))
                {
                    break;
                }
            }
            TryWaitForLoginKey();
        }

        #region Logging into Steam

        private void ConnectToSteam()
        {
            _steamClient.Connect();
            while (!_steamClient.IsConnected)
            {
                _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }
        
        //TODO document
        public void ConfigureLoginDetails(string username)
        {
            if (String.IsNullOrEmpty(username))
            {
                //TODO better exception
                throw new Exception("Username cannot be null or empty!");
            }

            string loginKey;
            AccountSettingsStore.Instance.LoginKeys.TryGetValue(username, out loginKey);

            _logonDetails = new SteamUser.LogOnDetails
            {
                Username = username,
                //TODO should clear out the password once we're done logging in, for security
                Password = loginKey == null ? Util.ReadPassword() : null,
                ShouldRememberPassword = true,
                LoginKey = loginKey,
                LoginID = 0x534DD2
            };
            // Sentry file is required when using Steam Guard w\ email
            if (AccountSettingsStore.Instance.SentryData.TryGetValue(_logonDetails.Username, out var bytes))
            {
                _logonDetails.SentryFileHash = bytes.ToShaHash();
            }
        }

        //TODO document
        private SteamUser.LoggedOnCallback _loggedOnCallbackResult;
        private SteamUser.LoggedOnCallback AttemptSteamLogin()
        {
            var loginTimeoutAfter = DateTime.Now.AddSeconds(30);

            _loggedOnCallbackResult = null;
            _steamUser.LogOn(_logonDetails);

            // Busy waiting for the callback to complete, then we can return the callback value synchronously
            while (_loggedOnCallbackResult == null)
            {
                _callbackManager.RunWaitCallbacks(timeout: TimeSpan.FromSeconds(3));
                if (DateTime.Now > loginTimeoutAfter)
                {
                    //TODO better exception
                    _ansiConsole.WriteLine("Timeout connecting to Steam3.");
                    throw new Exception("aborted");
                }
            }
            return _loggedOnCallbackResult;
        }

        //TODO document
        //TODO handle InvalidPassword
        private bool HandleLogonResult(SteamUser.LoggedOnCallback logonResult)
        {
            var loggedOn = logonResult;
            // If the account has 2-Factor login enabled, then we will need to re-login with the supplied code
            if (loggedOn.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                _logonDetails.TwoFactorCode = _ansiConsole.Prompt(new TextPrompt<string>(Yellow("2FA required for login.") +
                                                                                         $"  Please enter your {Cyan("Steam Guard code")} from your authenticator app : "));
                return false;
            }
            if (loggedOn.Result == EResult.TwoFactorCodeMismatch)
            {
                _logonDetails.TwoFactorCode = _ansiConsole.Prompt(new TextPrompt<string>(Red("Login failed. Incorrect Steam Guard code") +
                                                                                         "  Please try again : "));
                return false;
            }

            var loginKeyExpired = _logonDetails.LoginKey != null && loggedOn.Result == EResult.InvalidPassword;
            if (loginKeyExpired)
            {
                AccountSettingsStore.Instance.LoginKeys.Remove(_logonDetails.Username);
                AccountSettingsStore.Save();
                _logonDetails.LoginKey = null;

                _ansiConsole.Write("Login key was expired. Please enter your password: ");
                //TODO should clear out the password once we're done logging in, for security
                _logonDetails.Password = Util.ReadPassword();
                return false;
            }

            // SteamGuard code required
            if (loggedOn.Result == EResult.AccountLogonDenied)
            {
                _logonDetails.AuthCode = _ansiConsole.Prompt(new TextPrompt<string>(Yellow("This account is protected by Steam Guard.") +
                                                                                    "  Please enter the code sent to your email address:  "));
                return false;
            }

            if (loggedOn.Result == EResult.ServiceUnavailable)
            {
                _ansiConsole.WriteLine($"{Red("Unable to login to Steam")} : Service is unavailable");
                //TODO better exception type
                throw new Exception($"{Red("Unable to login to Steam")} : Service is unavailable");
            }

            if (loggedOn.Result != EResult.OK)
            {
                _ansiConsole.WriteLine($"Unable to login to Steam3: {loggedOn.Result}");
                throw new Exception("aborted");
            }

            _ansiConsole.LogMarkupLine($"Logged '{Cyan(_logonDetails.Username)}' into Steam3...");

            //TODO test this
            AppConfig.CellID = (int) loggedOn.CellID;
            return true;
        }
        
        bool _receivedLoginKey;
        //TODO cleanup + comment
        public void TryWaitForLoginKey()
        {
            if (_logonDetails.LoginKey != null)
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
                _callbackManager.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(100));
            }
        }

        private void LoginKeyCallback(SteamUser.LoginKeyCallback loginKey)
        {
            AccountSettingsStore.Instance.LoginKeys[_logonDetails.Username] = loginKey.LoginKey;
            AccountSettingsStore.Save();

            _steamUser.AcceptNewLoginKey(loginKey);
            _receivedLoginKey = true;
        }
        
        #endregion

        #region Other Auth Methods
        
        /// <summary>
        /// The UpdateMachineAuth event will be triggered once the user has logged in with either Steam Guard or 2FA enabled.
        /// This callback handler will save a "sentry file" for future logins, that will allow an existing Steam session to be reused,
        /// without requiring a password.
        ///
        /// Despite the fact that this will be triggered for both Steam Guard + 2FA, the sentry file is only required for re-login when using an
        /// account with Steam Guard enabled.
        /// </summary>
        private void UpdateMachineAuthCallback(SteamUser.UpdateMachineAuthCallback machineAuth)
        {
            AccountSettingsStore.Instance.SentryData[_logonDetails.Username] = machineAuth.Data;
            AccountSettingsStore.Save();

            var authResponse = new SteamUser.MachineAuthDetails
            {
                BytesWritten = machineAuth.BytesToWrite,
                FileName = machineAuth.FileName,
                FileSize = machineAuth.BytesToWrite,
                Offset = machineAuth.Offset,
                // should be the sha1 hash of the sentry file we just received
                SentryFileHash = machineAuth.Data.ToShaHash(),
                OneTimePassword = machineAuth.OneTimePassword,
                LastError = 0,
                Result = EResult.OK,
                JobID = machineAuth.JobID
            };
            _steamUser.SendMachineAuthResponse(authResponse);
        }
        
        #endregion

        #region LoadAccountLicenses

        private bool _loadAccountLicensesIsRunning = true;
        public void LoadAccountLicenses()
        {
            while (_loadAccountLicensesIsRunning)
            {
                _callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
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