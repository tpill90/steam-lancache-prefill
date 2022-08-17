using SteamPrefill.Utils;

namespace LancachePrefill.Common
{
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default)]
    [JsonSerializable(typeof(List<GithubRelease>))]
    internal partial class SerializationContext : JsonSerializerContext
    {
    }
}
