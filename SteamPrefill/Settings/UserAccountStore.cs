namespace SteamPrefill.Settings
{
    /// <summary>
    /// Keeps track of the session tokens returned by Steam, that allow for subsequent logins without passwords.
    /// </summary>
    [ProtoContract]
    public sealed class UserAccountStore
    {
        /// <summary>
        /// SentryData is returned by Steam when logging in with Steam Guard w\ email.
        /// This data is required to be passed along in every subsequent login, in order to re-use an existing session.
        /// </summary>
        [ProtoMember(1)]
        public Dictionary<string, byte[]> SentryData { get; private set; }

        /// <summary>
        /// Upon a successful login to Steam, a "Login Key" will be returned to use on subsequent logins.
        /// This login key can be considered a "session token", and can be used on subsequent logins to avoid entering a password.
        /// These keys will be unique to each user.
        /// </summary>
        [ProtoMember(2)]
        public Dictionary<string, string> LoginKeys { get; private set; }

        //TODO can I restrict using this getter? since there is already a method
        [ProtoMember(3)]
        public string CurrentUsername { get; private set; }

        private UserAccountStore()
        {
            SentryData = new Dictionary<string, byte[]>();
            LoginKeys = new Dictionary<string, string>();
        }

        public async Task<string> GetUsernameAsync(IAnsiConsole ansiConsole)
        {
            if (!String.IsNullOrEmpty(CurrentUsername))
            {
                return CurrentUsername;
            }

            CurrentUsername = await PromptForUsernameAsync(ansiConsole).WaitAsync(TimeSpan.FromSeconds(30));
            return CurrentUsername;
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

        public static UserAccountStore LoadFromFile()
        {
            if (!File.Exists(AppConfig.AccountSettingsStorePath))
            {
                return new UserAccountStore();
            }

            using var fileStream = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Open, FileAccess.Read);
            return Serializer.Deserialize<UserAccountStore>(fileStream);
        }

        public void Save()
        {
            using var fs = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Create, FileAccess.Write);
            Serializer.Serialize(fs, this);
        }
    }
}
