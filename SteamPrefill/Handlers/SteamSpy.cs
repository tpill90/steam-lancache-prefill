namespace SteamPrefill.Handlers
{
    public static class SteamSpy
    {
        public static async Task<List<SteamSpyApp>> TopGamesLast2WeeksAsync(IAnsiConsole ansiConsole)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };

                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://steamspy.com/api.php?request=top100forever"));
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                List<SteamSpyApp> topGames = JsonSerializer.Deserialize(responseContent, SerializationContext.Default.DictionaryStringSteamSpyApp)
                                                           .Select(e => e.Value)
                                                           .OrderByDescending(e => e.ccu)
                                                           .ToList();

                return topGames;
            }
            catch
            {
                ansiConsole.LogMarkupLine(Red("An unexpected error occurred while retrieving most played games from SteamSpy!  Popular games will be excluded from this prefill run."));
                return new List<SteamSpyApp>();
            }
        }
    }

    public sealed class SteamSpyApp
    {
        public uint appid { get; set; }
        public string name { get; set; }

        public int ccu { get; set; }

        public override string ToString()
        {
            return $"{name} - {ccu}";
        }
    }
}
