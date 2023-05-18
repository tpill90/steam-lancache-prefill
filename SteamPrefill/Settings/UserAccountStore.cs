using System.IdentityModel.Tokens.Jwt;

namespace SteamPrefill.Settings
{
    /// <summary>
    /// Keeps track of the auth tokens (JWT) returned by Steam, that allow for subsequent logins without passwords.
    /// </summary>
    [ProtoContract(SkipConstructor = true)]
    public sealed class UserAccountStore
    {
        //TODO deprecated, remove in the future, say 2023/07/01
        [ProtoMember(1)]
        public Dictionary<string, byte[]> SentryData { get; private set; }

        //TODO deprecated, remove in the future, say 2023/07/01
        [ProtoMember(2)]
        public Dictionary<string, string> SessionTokens { get; private set; }

        //TODO can I restrict using this getter? since there is already a method
        [ProtoMember(3)]
        public string CurrentUsername { get; private set; }

        /// <summary>
        /// Used to identify separate instances of Steam/SteamPrefill on the Steam network.
        /// As long as these don't collide, multiple instances can be logged in without logging each other out.
        /// </summary>
        [ProtoMember(4)]
        public uint? SessionId { get; private set; }

        /// <summary>
        /// Steam has switched over to using JWT tokens for authorization.
        /// </summary>
        [ProtoMember(5)]
        public string AccessToken { get; set; }

        [SuppressMessage("Security", "CA5394:Random is an insecure RNG", Justification = "Security doesn't matter here, as all that is needed is a unique id.")]
        private UserAccountStore()
        {
            SentryData = new Dictionary<string, byte[]>();
            SessionTokens = new Dictionary<string, string>();

            var random = new Random();
            SessionId = (uint)random.Next(0, 16384);
        }

        /// <summary>
        /// Gets the current user's username, if they have already entered it before.
        /// If they have not yet entered it, they will be prompted to do so.
        ///
        /// Will timeout after 30 seconds of no user activity.
        /// </summary>
        public async Task<string> GetUsernameAsync(IAnsiConsole ansiConsole)
        {
            if (!String.IsNullOrEmpty(CurrentUsername))
            {
                return CurrentUsername;
            }

            CurrentUsername = await PromptForUsernameAsync(ansiConsole).WaitAsync(TimeSpan.FromSeconds(30));
            return CurrentUsername;
        }

        public bool AccessTokenIsValid()
        {
            if (String.IsNullOrEmpty(AccessToken))
            {
                return false;
            }

            var parsedToken = new JwtSecurityToken(AccessToken);

            // Tokens seem to be valid for ~6 months.  We're going to add a bit of "buffer" (1 day) to make sure that new tokens are request prior to expiration
            var tokenHasExpired = DateTimeOffset.Now.DateTime.AddDays(1) < parsedToken.ValidTo;
            return tokenHasExpired;
        }

        private async Task<string> PromptForUsernameAsync(IAnsiConsole ansiConsole)
        {
            return await Task.Run(() =>
            {
                ansiConsole.MarkupLine($"A {Cyan("Steam")} account is required in order to prefill apps!");

                var prompt = new TextPrompt<string>($"Please enter your {Cyan("Steam account name")} : ")
                {
                    PromptStyle = new Style(SpectreColors.MediumPurple1)
                };
                return ansiConsole.Prompt(prompt);
            });
        }

        #region Serialization

        //TODO this adds about 100ms to each run
        public static UserAccountStore LoadFromFile()
        {
            if (!File.Exists(AppConfig.AccountSettingsStorePath))
            {
                return new UserAccountStore();
            }

            using var fileStream = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Open, FileAccess.Read);
            var userAccountStore = ProtoBuf.Serializer.Deserialize<UserAccountStore>(fileStream);
            return userAccountStore;
        }

        public void Save()
        {
            using var fs = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Create, FileAccess.Write);
            ProtoBuf.Serializer.Serialize(fs, this);
        }

        #endregion
    }
}