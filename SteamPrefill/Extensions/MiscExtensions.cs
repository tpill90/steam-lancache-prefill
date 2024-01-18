namespace SteamPrefill.Utils
{
    public static class MiscExtensions
    {
        //TODO rename to IsEmpty
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

        //TODO comment
        public static void WriteDebugFiles(this KeyValue rootKeyValue, string path)
        {
            if (!AppConfig.EnableSteamKitDebugLogs)
            {
                return;
            }

            if (File.Exists(path))
            {
                return;
            }

            rootKeyValue.SaveToFile(path, false);
        }
    }
}