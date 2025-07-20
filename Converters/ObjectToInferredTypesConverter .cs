using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linh.JsonKit.Converters;

/// <summary>
/// Efficient converter for deserializing JSON into .NET dynamic types with low allocations.
/// </summary>
public sealed class ObjectToInferredTypesConverter : JsonConverter<object>
{
    #region Read

    /// <inheritdoc/>
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ReadValue(ref reader, options);
    }

    private static object? ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.TryGetInt64(out long l) ? l : reader.GetDouble(),
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.StartObject => ReadObject(ref reader, options),
            JsonTokenType.StartArray => ReadArray(ref reader, options),
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };
    }

    private static Dictionary<string, object?> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        Dictionary<string, object?> result = new();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            string propertyName = reader.GetString()!;
            reader.Read();
            result[propertyName] = ReadValue(ref reader, options);
        }

        return result;
    }

    private static List<object?> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        const int InitialSize = 16;
        var pool = ArrayPool<object?>.Shared;
        object?[] buffer = pool.Rent(InitialSize);
        int count = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (count >= buffer.Length)
            {
                object?[] newBuffer = pool.Rent(buffer.Length * 2);
                Array.Copy(buffer, newBuffer, buffer.Length);
                pool.Return(buffer, clearArray: true);
                buffer = newBuffer;
            }

            buffer[count++] = ReadValue(ref reader, options);
        }

        var result = new List<object?>(count);
        for (int i = 0; i < count; i++)
            result.Add(buffer[i]);

        pool.Return(buffer, clearArray: true);
        return result;
    }

    #endregion

    #region Write

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        var innerOptions = new JsonSerializerOptions(options);
        var selfConverter = innerOptions.Converters.FirstOrDefault(c => c is ObjectToInferredTypesConverter);
        if (selfConverter != null) { innerOptions.Converters.Remove(selfConverter); }
        JsonSerializer.Serialize(writer, value, value.GetType(), innerOptions);
    }

    #endregion
}