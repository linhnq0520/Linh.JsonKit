using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linh.JsonKit.Converters;

/// <summary>
/// Adapter to allow Linh.JsonKit to work seamlessly with System.Text.Json Source Generator
/// by wrapping its logic into a compile-time compatible JsonConverter.
/// </summary>
public sealed class LinhJsonConverter<T> : JsonConverter<T>
{
    /// <inheritdoc/>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var json = doc.RootElement.GetRawText();
        return JConvert.FromJson<T>(json);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var json = JConvert.ToJson(value!);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.WriteTo(writer);
    }
}