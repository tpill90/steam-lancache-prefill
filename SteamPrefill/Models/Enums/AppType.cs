using Utf8Json;

namespace SteamPrefill.Models.Enums
{
    /// <summary>
    /// Steam docs:
    /// https://partner.steamgames.com/doc/api/steam_api?language=english#EAppType
    /// </summary>
    public class AppType : EnumBase<AppType>
    {
        public static readonly AppType Application = new AppType("application");
        public static readonly AppType Beta = new AppType("beta");
        public static readonly AppType Config = new AppType("config");
        public static readonly AppType Demo = new AppType("demo");
        public static readonly AppType Dlc = new AppType("dlc");
        public static readonly AppType Game = new AppType("game");
        public static readonly AppType Guide = new AppType("guide");
        public static readonly AppType Media = new AppType("media");
        public static readonly AppType Music = new AppType("music");
        public static readonly AppType Series = new AppType("series");
        public static readonly AppType Tool = new AppType("tool");
        public static readonly AppType Video = new AppType("video");

        private AppType(string name) : base(name)
        {
        }
    }

    /// <summary>
    /// Used to override the default serialization/deserialization behavior of Utf8Json
    /// </summary>
    public sealed class AppTypeFormatter : IJsonFormatter<AppType>, IObjectPropertyNameFormatter<AppType>
    {
        public void Serialize(ref JsonWriter writer, AppType value, IJsonFormatterResolver formatterResolver)
        {
            writer.WriteString(value.ToString());
        }

        public AppType Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return AppType.Parse(reader.ReadString());
        }

        public void SerializeToPropertyName(ref JsonWriter writer, AppType value, IJsonFormatterResolver formatterResolver)
        {
            writer.WriteString(value.ToString());
        }

        public AppType DeserializeFromPropertyName(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return AppType.Parse(reader.ReadString());
        }
    }
}
