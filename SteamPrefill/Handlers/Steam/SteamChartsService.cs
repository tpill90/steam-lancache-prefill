namespace SteamPrefill.Handlers.Steam
{
    public static class SteamChartsService
    {
        /// <summary>
        /// https://steamapi.xpaw.me/#ISteamChartsService/GetMostPlayedGames
        /// </summary>
        public static async Task<List<MostPlayedGame>> MostPlayedByDailyPlayersAsync(IAnsiConsole ansiConsole)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("https://api.steampowered.com/ISteamChartsService/GetMostPlayedGames/v1/"));
                var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                List<MostPlayedGame> topGames = JsonSerializer.Deserialize(responseContent, SerializationContext.Default.GetMostPlayedGamesResponse)
                                                              .response
                                                              .Ranks
                                                              .OrderBy(e => e.Rank)
                                                              .ToList();
                return topGames;
            }
            catch
            {
                ansiConsole.LogMarkupLine(Red("An unexpected error occurred while retrieving most played games!  Popular games will be excluded from this prefill run."));
                return new List<MostPlayedGame>();
            }
        }
    }

    #region Models

    public sealed class GetMostPlayedGamesResponse
    {
        public Response response { get; set; }
    }

    [SuppressMessage("Usage", "CA2227:Change to be read-only by removing property setter", Justification = "Properties must have setters for source generator deserializer to work")]
    public sealed class Response
    {
        [JsonPropertyName("ranks")]
        public List<MostPlayedGame> Ranks { get; set; }
    }

    public sealed class MostPlayedGame
    {
        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("appid")]
        public uint AppId { get; set; }

        public override string ToString()
        {
            return $"{AppId} - {Rank}";
        }
    }

    #endregion
}