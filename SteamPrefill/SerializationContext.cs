namespace SteamPrefill
{
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(List<GithubRelease>))]
    [JsonSerializable(typeof(List<uint>))]
    [JsonSerializable(typeof(HashSet<uint>))]
    [JsonSerializable(typeof(Dictionary<uint, HashSet<ulong>>))]
    internal partial class SerializationContext : JsonSerializerContext
    {
    }
}