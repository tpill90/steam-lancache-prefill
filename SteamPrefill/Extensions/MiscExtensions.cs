namespace SteamPrefill.Extensions
{
    public static class MiscExtensions
    {
        public static bool Empty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }

        public static ConcurrentStack<T> ToConcurrentStack<T>(this IEnumerable<T> list)
        {
            return new ConcurrentStack<T>(list);
        }

        public static void AddRange<T>(this HashSet<T> hashSet, List<T> values)
        {
            foreach (var value in values)
            {
                hashSet.Add(value);
            }
        }

        [SuppressMessage("Security", "CA5394:Random is an insecure RNG", Justification = "Security doesn't matter here, just need to shuffle requests.")]
        public static void Shuffle<T>(this IList<T> list)
        {
            var random = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static async Task<string> ReadPasswordAsync(this IAnsiConsole console, string promptText = null)
        {
            var promptTask = Task.Run(() =>
            {
                var defaultPrompt = $"Please enter your {Cyan("Steam password")}. {LightYellow("(Password won't be saved)")} : ";
                return console.Prompt(new TextPrompt<string>(promptText ?? defaultPrompt)
                                      .PromptStyle("white")
                                      .Secret());
            });
            return await promptTask.WaitAsync(TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Steam returns a large amount of metadata, which can be difficult to sift through using the debugger.  This metadata will be
        /// dumped to disk, so that it can be viewed in a text editor easily.
        /// </summary>
        public static void WriteSteamMetadataToDisk(this KeyValue rootKeyValue, string filePath)
        {
            if (!AppConfig.DebugLogs)
            {
                return;
            }
            if (File.Exists(filePath))
            {
                return;
            }

            var rootDir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(rootDir))
            {
                Directory.CreateDirectory(rootDir);
            }

            rootKeyValue.SaveToFile(filePath, false);
        }
    }
}