using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Linh.JsonKit.Json
{
    /// <summary>
    /// Provides a comprehensive and flexible toolkit for serializing and deserializing JSON data.
    /// - Supports various property naming conventions: Unchanged, PascalCase, camelCase, snake_case, kebab-case.
    /// - Automatically detects and parses dynamic structures into native .NET types when deserializing.
    /// - Smart handling of 'object' and 'Dictionary<string, object>' without falling back to 'JsonElement' or 'ValueKind'.
    /// - Configurable reference loop handling for serialization: Throw, Ignore, or Preserve.
    /// </summary>
    public static class JConvert
    {
        /// <summary>
        /// Defines supported naming conventions for property names during serialization.
        /// </summary>
        public enum NamingConvention
        {
            /// <summary>
            /// Keeps property names unchanged from the source object.
            /// </summary>
            Unchanged,

            /// <summary>
            /// Converts property names to PascalCase format.
            /// </summary>
            PascalCase,

            /// <summary>
            /// Converts property names to camelCase format.
            /// </summary>
            CamelCase,

            /// <summary>
            /// Converts property names to snake_case format (lowercase).
            /// </summary>
            SnakeCaseLower,

            /// <summary>
            /// Converts property names to kebab-case format (lowercase).
            /// </summary>
            KebabCaseLower,
        }

        /// <summary>
        /// Specifies how reference loops (cyclic object graphs) are handled during JSON serialization.
        /// </summary>
        public enum ReferenceLoopHandling
        {
            /// <summary>
            /// Throws a <see cref="JsonException"/> if a reference loop is detected.
            /// This is the safest option to avoid unexpected infinite recursion.
            /// </summary>
            Throw,

            /// <summary>
            /// Ignores the loop by skipping the repeated reference.
            /// The property causing the loop will be serialized as null.
            /// </summary>
            Ignore,

            /// <summary>
            /// Preserves the reference by writing metadata (e.g., $id and $ref)
            /// to maintain object identity in the JSON.
            /// </summary>
            Preserve,
        }

        /// <summary>
        /// Serializes any .NET object into a JSON string using the specified naming convention and reference loop handling.
        /// </summary>
        /// <param name="source">
        /// The source object to serialize. Can be a regular object, Dictionary, JToken, JsonNode, etc.
        /// If null, the result will be "null".
        /// </param>
        /// <param name="convention">
        /// The naming policy to apply to properties and dictionary keys during serialization.
        /// </param>
        /// <param name="loopHandling">
        /// Specifies how to handle reference loops during serialization.
        /// </param>
        /// <param name="writeIndented">
        /// Indicates whether to pretty-print the output JSON.
        /// </param>
        /// <returns>
        /// A JSON string representing the input object, formatted according to the selected convention.
        /// </returns>
        public static string ToJson(
            this object? source,
            NamingConvention convention = NamingConvention.Unchanged,
            ReferenceLoopHandling loopHandling = ReferenceLoopHandling.Throw,
            bool writeIndented = false
        )
        {
            if (source == null)
                return string.Empty;

            var key = (convention, loopHandling);
            var baseOptions = SerializationOptionsCache[key];
            var finalOptions =
                baseOptions.WriteIndented == writeIndented
                    ? baseOptions
                    : new JsonSerializerOptions(baseOptions) { WriteIndented = writeIndented };

            return JsonSerializer.Serialize(source, finalOptions);
        }

        /// <summary>
        /// Serializes a Newtonsoft.Json JToken into a JSON string using System.Text.Json.
        /// </summary>
        /// <param name="token">The JToken to serialize.</param>
        /// <param name="convention">The naming policy to apply to properties and dictionary keys.</param>
        /// <param name="loopHandling">Specifies how to handle reference loops during serialization.</param>
        /// <param name="writeIndented">Indicates whether to pretty-print the output JSON.</param>
        /// <returns>A JSON string representing the JToken.</returns>
        public static string ToJson(
            this JToken? token,
            NamingConvention convention,
            ReferenceLoopHandling loopHandling = ReferenceLoopHandling.Throw,
            bool writeIndented = false
        )
        {
            if (token == null)
                return "null";

            var settings = GetCachedSettings(convention, loopHandling, writeIndented);
            return Newtonsoft.Json.JsonConvert.SerializeObject(token, settings);
        }

        private static readonly Dictionary<
            (NamingConvention, ReferenceLoopHandling, bool),
            Newtonsoft.Json.JsonSerializerSettings
        > CachedSettings = [];

        private static Newtonsoft.Json.JsonSerializerSettings GetCachedSettings(
            NamingConvention convention,
            ReferenceLoopHandling loopHandling,
            bool writeIndented
        )
        {
            var key = (convention, loopHandling, writeIndented);
            if (CachedSettings.TryGetValue(key, out var cached))
                return cached;

            var formatting = writeIndented
                ? Newtonsoft.Json.Formatting.Indented
                : Newtonsoft.Json.Formatting.None;

            Newtonsoft.Json.Serialization.NamingStrategy? strategy = convention switch
            {
                NamingConvention.CamelCase =>
                    new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
                NamingConvention.SnakeCaseLower =>
                    new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy(),
                NamingConvention.KebabCaseLower =>
                    new Newtonsoft.Json.Serialization.KebabCaseNamingStrategy(),
                NamingConvention.PascalCase =>
                    new Newtonsoft.Json.Serialization.DefaultNamingStrategy(), // Newtonsoft không có PascalCase nên giữ nguyên
                _ => null,
            };

            var resolver =
                strategy != null
                    ? new Newtonsoft.Json.Serialization.DefaultContractResolver
                    {
                        NamingStrategy = strategy,
                    }
                    : null;

            var referenceHandling = loopHandling switch
            {
                ReferenceLoopHandling.Ignore => Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                ReferenceLoopHandling.Preserve => Newtonsoft.Json.ReferenceLoopHandling.Serialize,
                _ => Newtonsoft.Json.ReferenceLoopHandling.Error,
            };

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Formatting = formatting,
                ContractResolver = resolver,
                ReferenceLoopHandling = referenceHandling,
            };

            CachedSettings[key] = settings;
            return settings;
        }

        /// <summary>
        /// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
        /// Supports smart parsing of objects and dictionaries into native .NET types.
        /// </summary>
        /// <typeparam name="T">
        /// The target .NET type to deserialize into.
        /// </typeparam>
        /// <param name="json">
        /// The JSON string to parse. If null, empty, or "null", the result will be default(T).
        /// </param>
        /// <returns>
        /// An object of type T deserialized from the JSON string, or default(T) if input is invalid.
        /// </returns>
        public static T? FromJson<T>(this string? json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "null")
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, DeserializationOptions);
        }

        private static readonly Dictionary<
            (NamingConvention, ReferenceLoopHandling),
            JsonSerializerOptions
        > SerializationOptionsCache = [];
        private static readonly JsonSerializerOptions DeserializationOptions;

        static JConvert()
        {
            var recursiveConverters = new JsonConverter[]
            {
                new RecursiveJTokenConverter(),
                new RecursiveJsonNodeConverter(),
            };

            // Configure serialization options for each combination of NamingConvention and ReferenceLoopHandling
            foreach (NamingConvention convention in Enum.GetValues<NamingConvention>())
            {
                foreach (
                    ReferenceLoopHandling loopHandling in Enum.GetValues<ReferenceLoopHandling>()
                )
                {
                    JsonNamingPolicy? policy = convention switch
                    {
                        NamingConvention.Unchanged => null,
                        NamingConvention.PascalCase => new PascalCaseNamingPolicy(),
                        NamingConvention.CamelCase => JsonNamingPolicy.CamelCase,
                        NamingConvention.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
                        NamingConvention.KebabCaseLower => new KebabCaseLowerNamingPolicy(),
                        _ => throw new ArgumentOutOfRangeException(nameof(convention)),
                    };

                    ReferenceHandler? referenceHandler = loopHandling switch
                    {
                        ReferenceLoopHandling.Throw => null,
                        ReferenceLoopHandling.Ignore => ReferenceHandler.IgnoreCycles,
                        ReferenceLoopHandling.Preserve => ReferenceHandler.Preserve,
                        _ => throw new ArgumentOutOfRangeException(nameof(loopHandling)),
                    };

                    SerializationOptionsCache[(convention, loopHandling)] = CreateOptions(
                        policy,
                        referenceHandler,
                        recursiveConverters
                    );
                }
            }

            // Configure deserialization options
            DeserializationOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve, // Support $id/$ref during deserialization
                Converters =
                {
                    new ObjectToInferredTypesConverter(),
                    new RecursiveJTokenConverter(),
                    new RecursiveJsonNodeConverter(),
                },
            };
        }

        private static JsonSerializerOptions CreateOptions(
            JsonNamingPolicy? policy,
            ReferenceHandler? referenceHandler,
            params JsonConverter[] converters
        )
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = policy,
                DictionaryKeyPolicy = policy,
                WriteIndented = false,
                ReferenceHandler = referenceHandler,
            };

            foreach (var converter in converters)
            {
                options.Converters.Add(converter);
            }

            return options;
        }

        private class KebabCaseLowerNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;
                var sb = new StringBuilder();
                sb.Append(char.ToLowerInvariant(name[0]));
                for (int i = 1; i < name.Length; ++i)
                {
                    char c = name[i];
                    if (char.IsUpper(c))
                    {
                        sb.Append('-');
                        sb.Append(char.ToLowerInvariant(c));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
        }

        private class PascalCaseNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;

                // Split words based on '_' or '-'
                string[] parts = name.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    return name;
                if (parts.Length == 1 && char.IsUpper(name[0]))
                    return name;

                var sb = new StringBuilder();
                foreach (string part in parts)
                {
                    if (string.IsNullOrEmpty(part))
                        continue;
                    sb.Append(char.ToUpper(part[0], CultureInfo.InvariantCulture));
                    sb.Append(part.AsSpan(1));
                }

                if (sb.Length == 0 && name.Length > 0)
                {
                    sb.Append(char.ToUpper(name[0], CultureInfo.InvariantCulture));
                    sb.Append(name.AsSpan(1));
                }

                return sb.ToString();
            }
        }

        private class RecursiveJTokenConverter : JsonConverter<JToken>
        {
            public override void Write(
                Utf8JsonWriter writer,
                JToken value,
                JsonSerializerOptions options
            )
            {
                var namingPolicy = options.DictionaryKeyPolicy ?? options.PropertyNamingPolicy;
                WriteJToken(writer, value, options, namingPolicy);
            }

            private static void WriteJToken(
                Utf8JsonWriter writer,
                JToken token,
                JsonSerializerOptions options,
                JsonNamingPolicy? namingPolicy
            )
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
                            WriteJToken(writer, item, options, namingPolicy);
                        writer.WriteEndArray();
                        break;
                    default:
                        JsonSerializer.Serialize(writer, ((JValue)token).Value, options);
                        break;
                }
            }

            public override JToken Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options
            ) => JToken.Parse(JsonDocument.ParseValue(ref reader).RootElement.GetRawText());
        }

        private class RecursiveJsonNodeConverter : JsonConverter<JsonNode>
        {
            public override void Write(
                Utf8JsonWriter writer,
                JsonNode value,
                JsonSerializerOptions options
            )
            {
                var namingPolicy = options.DictionaryKeyPolicy ?? options.PropertyNamingPolicy;
                WriteJsonNode(writer, value, options, namingPolicy);
            }

            private static void WriteJsonNode(
                Utf8JsonWriter writer,
                JsonNode? node,
                JsonSerializerOptions options,
                JsonNamingPolicy? namingPolicy
            )
            {
                if (node is null)
                {
                    writer.WriteNullValue();
                    return;
                }
                switch (node)
                {
                    case JsonObject obj:
                        writer.WriteStartObject();
                        foreach (var prop in obj)
                        {
                            var propertyName = namingPolicy?.ConvertName(prop.Key) ?? prop.Key;
                            writer.WritePropertyName(propertyName);
                            WriteJsonNode(writer, prop.Value, options, namingPolicy);
                        }
                        writer.WriteEndObject();
                        break;
                    case JsonArray arr:
                        writer.WriteStartArray();
                        foreach (var item in arr)
                            WriteJsonNode(writer, item, options, namingPolicy);
                        writer.WriteEndArray();
                        break;
                    case JsonValue val:
                        val.WriteTo(writer, options);
                        break;
                }
            }

            public override JsonNode? Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options
            ) => JsonNode.Parse(ref reader);
        }

        private class ObjectToInferredTypesConverter : JsonConverter<object>
        {
            public override object? Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options
            )
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
                    _ => throw new JsonException(
                        $"Token type {reader.TokenType} is not supported."
                    ),
                };
            }

            private List<object?> ReadArray(
                ref Utf8JsonReader reader,
                JsonSerializerOptions options
            )
            {
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(Read(ref reader, typeof(object), options));
                }
                return list;
            }

            private Dictionary<string, object?> ReadObject(
                ref Utf8JsonReader reader,
                JsonSerializerOptions options
            )
            {
                var dict = new Dictionary<string, object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException("Expected a property name.");
                    string key = reader.GetString()!;
                    reader.Read();
                    dict[key] = Read(ref reader, typeof(object), options);
                }
                return dict;
            }

            public override void Write(
                Utf8JsonWriter writer,
                object value,
                JsonSerializerOptions options
            )
            {
                JsonSerializer.Serialize(writer, value, value.GetType(), options);
            }
        }
    }
}
