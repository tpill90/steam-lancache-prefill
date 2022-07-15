using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using ProtoBuf;
using Spectre.Console;

namespace DepotDownloader.Protos
{
    //TODO document this whole class
    [ProtoContract]
    public class AccountSettingsStore
    {
        //TODO document what this is for
        [ProtoMember(1, IsRequired = false)]
        public Dictionary<string, byte[]> SentryData { get; private set; }
        
        [ProtoMember(2, IsRequired = false)]
        public Dictionary<string, string> LoginKeys { get; private set; }

        public static AccountSettingsStore Instance;

        private AccountSettingsStore()
        {
            SentryData = new Dictionary<string, byte[]>();
            LoginKeys = new Dictionary<string, string>();
        }

        static bool Loaded => Instance != null;

        public static void LoadFromFile()
        {
            if (Loaded)
            {
                throw new Exception("Config already loaded");
            }

            var timer = Stopwatch.StartNew();
            if (!File.Exists(AppConfig.AccountSettingsStorePath))
            {
                Instance = new AccountSettingsStore();
                return;
            }

            using var fileStream = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Open, FileAccess.Read);
            Instance = Serializer.Deserialize<AccountSettingsStore>(fileStream);
            AnsiConsole.WriteLine(timer.ElapsedMilliseconds);
        }

        public static void Save()
        {
            if (!Loaded)
                throw new Exception("Saved config before loading");

            using var fs = File.Open(AppConfig.AccountSettingsStorePath, FileMode.Create, FileAccess.Write);
            //using var ds = new DeflateStream(fs, CompressionMode.Compress);
            Serializer.Serialize(fs, Instance);
        }
    }
}
