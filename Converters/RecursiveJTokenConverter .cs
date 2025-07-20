using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linh.JsonKit.Converters;

/// <summary>
/// Handles serialization and deserialization of Newtonsoft.Json.Linq.JToken.
/// Internal implementation detail for JConvert.
/// </summary>
internal sealed class RecursiveJTokenConverter : JsonConverter<JToken>
{
    public override JToken Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return JToken.Parse(doc.RootElement.GetRawText());
    }

    public override void Write(Utf8JsonWriter writer, JToken value, JsonSerializerOptions options)
    {
        // Use the naming policy from the options for consistency
        var namingPolicy = options.DictionaryKeyPolicy ?? options.PropertyNamingPolicy;
        WriteJToken(writer, value, options, namingPolicy);
    }

    private static void WriteJToken(Utf8JsonWriter writer, JToken token, JsonSerializerOptions options, JsonNamingPolicy? namingPolicy)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                writer.WriteStartObject();
                foreach (var prop in ((JObject)token).Properties())
                {
                    var propertyName = namingPolicy?.ConvertName(prop.Name) ?? prop.Name;
                    writer.WritePropertyName(propertyName);
                    WriteJToken(writer, prop.Value, options, namingPolicy);
                }
                writer.WriteEndObject();
                break;
            case JTokenType.Array:
                writer.WriteStartArray();
                foreach (var item in (JArray)token)
                {
                    WriteJToken(writer, item, options, namingPolicy);
                }
                writer.WriteEndArray();
                break;
            default:
                // For simple values, let System.Text.Json handle it
                JsonSerializer.Serialize(writer, ((JValue)token).Value, options);
                break;
        }
    }
}