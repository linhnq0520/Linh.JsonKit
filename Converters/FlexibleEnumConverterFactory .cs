// FILENAME: Linh.JsonKit/Converters/FlexibleEnumConverterFactory.cs
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linh.JsonKit.Converters;

/// <summary>
/// A flexible enum converter factory that allows deserializing enums from both string names and integer values.
/// By default, it serializes enums to their underlying integer value, preserving System.Text.Json's default behavior.
/// </summary>
public sealed class FlexibleEnumConverterFactory : JsonConverterFactory
{
    private readonly bool _serializeAsStrings;

    /// <summary>
    /// Initializes a new instance of the <see cref="FlexibleEnumConverterFactory"/> class.
    /// </summary>
    /// <param name="serializeAsStrings">If true, enums will be serialized as their string names. If false, they will be serialized as numbers. Default is false.</param>
    public FlexibleEnumConverterFactory(bool serializeAsStrings = false)
    {
        _serializeAsStrings = serializeAsStrings;
    }

    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter)Activator.CreateInstance(
            typeof(FlexibleEnumConverter<>).MakeGenericType(typeToConvert),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: [_serializeAsStrings],
            culture: null)!;
    }

    private class FlexibleEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        private readonly bool _serializeAsStrings;
        private readonly TypeCode _enumTypeCode;

        public FlexibleEnumConverter(bool serializeAsStrings)
        {
            _serializeAsStrings = serializeAsStrings;
            _enumTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum)));
        }

        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    string? stringValue = reader.GetString() ?? throw new JsonException($"Cannot convert null to enum {typeof(TEnum).Name}.");

                    if (Enum.TryParse(stringValue, ignoreCase: true, out TEnum result))
                        return result;

                    if (long.TryParse(stringValue, out long longValue))
                        return (TEnum)Enum.ToObject(typeToConvert, longValue);

                    throw new JsonException($"Cannot convert string '{stringValue}' to enum {typeof(TEnum).Name}.");

                case JsonTokenType.Number:
                    if (reader.TryGetInt64(out long numberValue))
                        return (TEnum)Enum.ToObject(typeToConvert, numberValue);

                    throw new JsonException($"Cannot convert number to enum {typeof(TEnum).Name}.");

                default:
                    throw new JsonException($"Unexpected token type {reader.TokenType} when parsing enum {typeof(TEnum).Name}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            if (_serializeAsStrings)
            {
                writer.WriteStringValue(value.ToString());
            }
            else
            {
                // Replicate the default behavior of serializing to a number
                switch (_enumTypeCode)
                {
                    case TypeCode.Int32:
                        writer.WriteNumberValue(Convert.ToInt32(value));
                        break;
                    case TypeCode.Int64:
                        writer.WriteNumberValue(Convert.ToInt64(value));
                        break;
                    case TypeCode.UInt32:
                        writer.WriteNumberValue(Convert.ToUInt32(value));
                        break;
                    case TypeCode.UInt64:
                        writer.WriteNumberValue(Convert.ToUInt64(value));
                        break;
                    case TypeCode.Int16:
                        writer.WriteNumberValue(Convert.ToInt16(value));
                        break;
                    case TypeCode.UInt16:
                        writer.WriteNumberValue(Convert.ToUInt16(value));
                        break;
                    case TypeCode.Byte:
                        writer.WriteNumberValue(Convert.ToByte(value));
                        break;
                    case TypeCode.SByte:
                        writer.WriteNumberValue(Convert.ToSByte(value));
                        break;
                    default:
                        throw new JsonException($"Unsupported enum underlying type: {_enumTypeCode}.");
                }
            }
        }
    }
}