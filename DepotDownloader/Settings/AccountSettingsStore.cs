using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace DepotDownloader.Settings
{
    /// <summary>
    /// Keeps track of the session tokens returned by Steam, that allow for subsequent logins without passwords.
    /// </summary>
    [ProtoContract]
    public class AccountSettingsStore
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

        public static AccountSettingsStore Instance;
        static bool Loaded => Instance != null;

        private AccountSettingsStore()
        {
            SentryData = new Dictionary<string, byte[]>();
            LoginKeys = new Dictionary<string, string>();
        }

        public static void LoadFromFile()
        {
            if (Loaded)
            {
                throw new Exception("Config already loaded");
            }

            if (!File.Exists(AppConfig.AccountSettingsStorePath))
            {
                Instance = new AccountSettingsStore();
                return;
            }

            using var fileStream = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Open, FileAccess.Read);
            Instance = Serializer.Deserialize<AccountSettingsStore>(fileStream);
        }

        public static void Save()
        {
            if (!Loaded)
            {
                throw new Exception("Saved config before loading");
            }

            using var fs = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Create, FileAccess.Write);
            Serializer.Serialize(fs, Instance);
        }
    }
}
