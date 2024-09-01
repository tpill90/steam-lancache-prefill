namespace SteamPrefill.Handlers.Steam
{
    public sealed class Steam3Session : IDisposable
    {
        /// <summary>
        /// CellId represents the region that the user is geographically located in, and determines which Connection Managers and CDNs
        /// will be used by SteamPrefill.
        ///
        /// Typically, Steam will automatically select the correct CellId using geolocation.
        /// However, the api endpoint used (ISteamDirectory/GetCMList) will unpredictably return non-local servers due to an issue with Valve's
        /// api not handling trailing slashes correctly.
        ///
        /// For example calling ISteamDirectory/GetCMList/v1?cellid=0 will always return the correct regional servers, however adding a trailing slash
        /// to the end of the url (ex. /v1/?) will cause Steam to return non-local servers.
        ///
        /// Upon login to the Steam network certain metadata about the session will be received, this includes the correct CellId which we will save
        /// and use for future logins.  Using the correct CellId will guarantee significantly faster login and app metadata retrieval times.
        ///
        /// See https://tpill90.github.io/steam-lancache-prefill/steam-docs/CDN-Regions/ for a list of known CDNs
        /// </summary>
        private uint CellId
        {
            get => File.Exists(AppConfig.CachedCellIdPath) ? uint.Parse(File.ReadAllText(AppConfig.CachedCellIdPath)) : 0;
            set => File.WriteAllText(AppConfig.CachedCellIdPath, value.ToString());
        }

        #region Member fields

        // Steam services
        private readonly SteamClient _steamClient;
        private readonly SteamUser _steamUser;
        public readonly SteamContent SteamContent;
        public readonly SteamApps SteamAppsApi;
        public readonly Client CdnClient;
        public SteamUnifiedMessages.UnifiedService<IPlayer> unifiedPlayerService;
        private readonly CallbackManager _callbackManager;

        private SteamUser.LogOnDetails _logonDetails;
        private readonly IAnsiConsole _ansiConsole;

        private readonly UserAccountStore _userAccountStore;
        public readonly LicenseManager LicenseManager;

        public SteamID _steamId;

        #endregion

        public Steam3Session(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;

            _steamClient = new SteamClient(SteamConfiguration.Create(e => e.WithCellID(CellId)
                                                                           // TODO remove this line when this PR is merged and deployed https://github.com/SteamRE/SteamKit/pull/1420
                                                                           .WithProtocolTypes(ProtocolTypes.WebSocket)
                                                                           .WithConnectionTimeout(TimeSpan.FromSeconds(10))));

            _steamUser = _steamClient.GetHandler<SteamUser>();
            SteamAppsApi = _steamClient.GetHandler<SteamApps>();
            SteamContent = _steamClient.GetHandler<SteamContent>();
            SteamUnifiedMessages steamUnifiedMessages = _steamClient.GetHandler<SteamUnifiedMessages>();
            unifiedPlayerService = steamUnifiedMessages.CreateService<IPlayer>();

            _callbackManager = new CallbackManager(_steamClient);

            // This callback is triggered when SteamKit2 makes a successful connection
            _callbackManager.Subscribe<SteamClient.ConnectedCallback>(e =>
            {
                _isConnecting = false;
                _disconnected = false;
            });
            // If a connection attempt fails in any way, SteamKit2 notifies of the failure with a "disconnect"
            _callbackManager.Subscribe<SteamClient.DisconnectedCallback>(e =>
            {
                _isConnecting = false;
                _disconnected = true;
            });

            _callbackManager.Subscribe<SteamUser.LoggedOnCallback>(loggedOn =>
            {
                _loggedOnCallbackResult = loggedOn;
                CellId = loggedOn.CellID;
            });
            _callbackManager.Subscribe<LicenseListCallback>(LicenseListCallback);

            CdnClient = new Client(_steamClient);
            // Configuring SteamKit's HttpClient to timeout in a more reasonable time frame.  This is only used when downloading manifests
            Client.RequestTimeout = TimeSpan.FromSeconds(60);

            _userAccountStore = UserAccountStore.LoadFromFile();
            LicenseManager = new LicenseManager(SteamAppsApi);
        }

        public async Task LoginToSteamAsync()
        {
            await ConfigureLoginDetailsAsync();

            int retryCount = 0;
            bool logonSuccess = false;
            while (!logonSuccess)
            {
                _callbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(50));

                SteamUser.LoggedOnCallback logonResult = null;
                await _ansiConsole.StatusSpinner().StartAsync("Connecting to Steam...", async ctx =>
                {
                    ConnectToSteam();

                    // Making sure that we have a valid access token before moving onto the login
                    ctx.Status = "Retrieving access token...";
                    await GetAccessTokenAsync();

                    ctx.Status = "Logging in to Steam...";
                    logonResult = AttemptSteamLogin();
                });

                logonSuccess = HandleLogonResult(logonResult);

                retryCount++;
                if (retryCount == 5)
                {
                    throw new SteamLoginException("Unable to login to Steam!  Try again in a few moments...");
                }
            }

            _ansiConsole.LogMarkupVerbose($"Connected to CM {LightYellow(_steamClient.CurrentEndPoint)}");
        }

        private async Task GetAccessTokenAsync()
        {
            if (_userAccountStore.AccessTokenIsValid())
            {
                return;
            }

            _ansiConsole.LogMarkupLine("Requesting new access token...");

            // Begin authenticating via credentials
            var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
            {
                Username = _logonDetails.Username,
                Password = _logonDetails.Password,
                IsPersistentSession = true,
                Authenticator = new UserConsoleAuthenticator()
            });

            // Starting polling Steam for authentication response
            var pollResponse = await authSession.PollingWaitForResultAsync();
            _userAccountStore.AccessToken = pollResponse.RefreshToken;
            _userAccountStore.Save();

            // Clearing password so it doesn't stay in memory
            _logonDetails.Password = null;
            GC.Collect();
        }

        private async Task ConfigureLoginDetailsAsync()
        {
            var username = await _userAccountStore.GetUsernameAsync(_ansiConsole);

            _logonDetails = new SteamUser.LogOnDetails
            {
                Username = username,
                ShouldRememberPassword = true,
                Password = _userAccountStore.AccessTokenIsValid() ? null : await _ansiConsole.ReadPasswordAsync(),
                LoginID = _userAccountStore.SessionId
            };
        }

        #region  Connecting to Steam

        // Used to busy wait until the connection attempt finishes in either a success or failure
        private bool _isConnecting;

        /// <summary>
        /// Attempts to establish a connection to the Steam network.
        /// Retries if necessary until successful connection is established
        /// </summary>
        /// <exception cref="SteamConnectionException">Throws if unable to connect to Steam</exception>
        private void ConnectToSteam()
        {
            _ansiConsole.LogMarkupVerbose($"Connecting with CellId: {LightYellow(CellId)}");
            var timeoutAfter = DateTime.Now.AddSeconds(30);

            // Busy waiting until the client has a successful connection established
            while (!_steamClient.IsConnected)
            {
                _isConnecting = true;
                _steamClient.Connect();

                // Busy waiting until SteamKit2 either succeeds/fails the connection attempt
                while (_isConnecting)
                {
                    _callbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(50));
                    if (DateTime.Now > timeoutAfter)
                    {
                        throw new SteamConnectionException("Timeout connecting to Steam...  Try again in a few moments");
                    }
                }
            }
            _ansiConsole.LogMarkupLine("Connected to Steam!");
        }

        #endregion

        #region Logging into Steam

        private SteamUser.LoggedOnCallback _loggedOnCallbackResult;
        private int _failedLogonAttempts;

        [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "while() loop is not infinite.  _loggedOnCallbackResult is set after logging into Steam")]
        private SteamUser.LoggedOnCallback AttemptSteamLogin()
        {
            var timeoutAfter = DateTime.Now.AddSeconds(30);
            // Need to reset this global result value, as it will be populated once the logon callback completes
            _loggedOnCallbackResult = null;

            _logonDetails.AccessToken = _userAccountStore.AccessToken;
            _steamUser.LogOn(_logonDetails);

            // Busy waiting for the callback to complete, then we can return the callback value synchronously
            while (_loggedOnCallbackResult == null)
            {
                _callbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(50));
                if (DateTime.Now > timeoutAfter)
                {
                    throw new SteamLoginException("Timeout logging into Steam...  Try again in a few moments");
                }
            }
            return _loggedOnCallbackResult;
        }

        [SuppressMessage("", "VSTHRD002:Synchronously waiting on tasks may cause deadlocks.", Justification = "Its not possible for this callback method to be async, must block synchronously")]
        private bool HandleLogonResult(SteamUser.LoggedOnCallback logonResult)
        {
            _steamId = logonResult.ClientSteamID;

            var loggedOn = logonResult;

            // If the account has 2-Factor login enabled, then we will need to re-login with the supplied code
            if (loggedOn.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                _logonDetails.TwoFactorCode = _ansiConsole.Prompt(new TextPrompt<string>(LightYellow("2FA required for login.") +
                                                                                         $"  Please enter your {Cyan("Steam Guard code")} from your authenticator app : "));
                return false;
            }
            if (loggedOn.Result == EResult.TwoFactorCodeMismatch)
            {
                _logonDetails.TwoFactorCode = _ansiConsole.Prompt(new TextPrompt<string>(Red("Login failed. Incorrect Steam Guard code") +
                                                                                         "  Please try again : "));
                return false;
            }

            if (loggedOn.Result == EResult.InvalidPassword)
            {
                _failedLogonAttempts++;
                if (_failedLogonAttempts == 3)
                {
                    _ansiConsole.LogMarkupLine(Red("Invalid username/password combination!  Check your login credential validity, and try again.."));
                    throw new AuthenticationException("Invalid username/password");
                }

                _logonDetails.Password = _ansiConsole.ReadPasswordAsync($"{Red("Invalid password!  Please re-enter your password!")}").GetAwaiter().GetResult();
                return false;
            }
            // User previously authenticated, but changed their password such that the previous access token is no longer valid.
            if (loggedOn.Result == EResult.AccessDenied)
            {
                _ansiConsole.LogMarkupLine(Red("Steam password was changed!  Current login token is no longer valid.  Re-authentication is required..."));
                _logonDetails.Password = _ansiConsole.ReadPasswordAsync($"{Red("Please enter your password!")}").GetAwaiter().GetResult();
                _userAccountStore.AccessToken = null;
                return false;
            }
            // SteamGuard code required
            if (loggedOn.Result == EResult.AccountLogonDenied)
            {
                _logonDetails.AuthCode = _ansiConsole.Prompt(new TextPrompt<string>(LightYellow("This account is protected by Steam Guard.") +
                                                                                    "  Please enter the code sent to your email address:  "));
                return false;
            }
            if (loggedOn.Result == EResult.ServiceUnavailable)
            {
                throw new SteamLoginException("Unable to login to Steam : Service is unavailable");
            }
            if (loggedOn.Result != EResult.OK)
            {
                throw new SteamLoginException($"Unable to login to Steam.  An unknown error occurred : {loggedOn.Result}");
            }

            _ansiConsole.LogMarkupLine("Logged into Steam");

            // Forcing a garbage collect to remove stored password from memory
            if (_logonDetails.Password != null)
            {
                _logonDetails.Password = null;
                GC.Collect(3, GCCollectionMode.Forced);
            }

            return true;
        }

        private bool _disconnected = true;
        public void Disconnect()
        {
            if (_disconnected)
            {
                // Needed so message doesn't display on the same line as the prompt
                _ansiConsole.WriteLine("");
                _ansiConsole.LogMarkupLine("Already disconnected from Steam");
                return;
            }

            _disconnected = false;
            _steamClient.Disconnect();

            _ansiConsole.StatusSpinner().Start("Disconnecting", context =>
            {
                while (!_disconnected)
                {
                    _callbackManager.RunWaitAllCallbacks(TimeSpan.FromMilliseconds(100));
                }
            });
            _ansiConsole.LogMarkupLine("Disconnected from Steam!");
        }

        #endregion

        #region LoadAccountLicenses

        private bool _loadAccountLicensesIsRunning = true;
        /// <summary>
        /// Waits for the user's currently owned licenses(games) to be returned.
        /// The license query is triggered on application startup, and requires busy-waiting to receive the callback
        /// </summary>
        public void WaitForLicenseCallback()
        {
            _ansiConsole.StatusSpinner().Start("Retrieving owned apps...", _ =>
            {
                while (_loadAccountLicensesIsRunning)
                {
                    _callbackManager.RunWaitAllCallbacks(timeout: TimeSpan.FromMilliseconds(50));
                }
            });
        }

        private void LicenseListCallback(LicenseListCallback licenseList)
        {
            var timer = Stopwatch.StartNew();

            _loadAccountLicensesIsRunning = false;
            if (licenseList.Result != EResult.OK)
            {
                _ansiConsole.MarkupLine(Red($"Unexpected error while retrieving license list : {licenseList.Result}"));
                throw new SteamLoginException("Unable to retrieve user licenses!");
            }
            LicenseManager.LoadPackageInfo(licenseList.LicenseList);

            _ansiConsole.LogMarkupLine("Loaded account licenses", timer);
        }

        #endregion

        public void Dispose()
        {
            CdnClient.Dispose();
        }
    }
}