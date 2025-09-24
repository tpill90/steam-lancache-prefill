namespace SteamPrefill.Handlers.Steam
{
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