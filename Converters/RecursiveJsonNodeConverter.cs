using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Linh.JsonKit.Converters;

/// <summary>
/// Handles serialization and deserialization of Newtonsoft.Json.Linq.JToken.
/// Internal implementation detail for JConvert.
/// </summary>
internal sealed class RecursiveJsonNodeConverter : JsonConverter<JsonNode>
{
    public override JsonNode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonNode.Parse(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, JsonNode value, JsonSerializerOptions options)
    {
        var namingPolicy = options.DictionaryKeyPolicy ?? options.PropertyNamingPolicy;
        if (namingPolicy == null) { value.WriteTo(writer, options); return; }
        WriteJsonNode(writer, value, options, namingPolicy);
    }

    private static void WriteJsonNode(Utf8JsonWriter writer, JsonNode? node, JsonSerializerOptions options, JsonNamingPolicy namingPolicy)
    {
        if (node == null) { writer.WriteNullValue(); return; }
        switch (node)
        {
            case JsonObject obj:
                writer.WriteStartObject();
                foreach (var prop in obj)
                {
                    writer.WritePropertyName(namingPolicy.ConvertName(prop.Key));
                    WriteJsonNode(writer, prop.Value, options, namingPolicy);
                }
                writer.WriteEndObject();
                break;
            case JsonArray arr:
                writer.WriteStartArray();
                foreach (var item in arr) { WriteJsonNode(writer, item, options, namingPolicy); }
                writer.WriteEndArray();
                break;
            default:
                node.WriteTo(writer, options);
                break;
        }
    }
}