using Linh.JsonKit.Json.Resolver;

namespace Linh.JsonKit.Converters
{
    internal sealed class RecursiveSpanJsonDynamicObjectConverter : System.Text.Json.Serialization.JsonConverter<SpanJson.Formatters.Dynamic.SpanJsonDynamicObject>
    {
        public override SpanJson.Formatters.Dynamic.SpanJsonDynamicObject? Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
        {
            using var doc = System.Text.Json.JsonDocument.ParseValue(ref reader);
            var rawJson = doc.RootElement.GetRawText();
            if (string.IsNullOrEmpty(rawJson) || rawJson == "null") return null;
            return SpanJson.JsonSerializer.Generic.Utf16.Deserialize<SpanJson.Formatters.Dynamic.SpanJsonDynamicObject, SpanJsonResolver<char>>(rawJson);
        }

        public override void Write(System.Text.Json.Utf8JsonWriter writer, SpanJson.Formatters.Dynamic.SpanJsonDynamicObject value, System.Text.Json.JsonSerializerOptions options)
        {
            if (value == null) { writer.WriteNullValue(); return; }
            var spanJsonString = SpanJson.JsonSerializer.Generic.Utf16.Serialize<SpanJson.Formatters.Dynamic.SpanJsonDynamicObject, SpanJsonResolver<char>>(value);
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(spanJsonString);
            jsonDoc.RootElement.WriteTo(writer);
        }
    }
}