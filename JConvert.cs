using Linh.JsonKit.Converters;
using Linh.JsonKit.Enums;
using Linh.JsonKit.Policies;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linh.JsonKit;

/// <summary>
/// Provides a flexible and high-performance toolkit for serializing and deserializing JSON data.
/// </summary>
public static partial class JConvert
{
    /// <summary>
    /// Serializes any .NET object into a JSON string using an optimized pipeline.
    /// </summary>
    /// <param name="source">The object to serialize.</param>
    /// <param name="configureOptions">An action to configure serialization options for this specific call.</param>
    /// <returns>A JSON string representation of the object.</returns>
    public static string ToJson(this object? source, Action<JConvertOptions>? configureOptions = null)
    {
        if (source == null) return "null";

        var options = new JConvertOptions();
        configureOptions?.Invoke(options);

        // Special path for Newtonsoft.Json.Linq.JToken, now fully consistent with options
        if (source is Newtonsoft.Json.Linq.JToken token)
        {
            var settings = GetNewtonsoftCachedSettings(
                options.NamingConvention,
                options.LoopHandling,
                options.WriteIndented,
                options.EnumSerialization);
            return Newtonsoft.Json.JsonConvert.SerializeObject(token, settings);
        }

        // Build the key for our cache, including EnumSerialization mode.
        var key = (options.NamingConvention, options.LoopHandling, options.EnumSerialization);
        var baseOptions = SerializationOptionsCache[key];

        // If only WriteIndented is different, create a new options instance. Otherwise, use the cached version.
        var finalOptions = new JsonSerializerOptions(baseOptions)
        {
            WriteIndented = options.WriteIndented
        };

        // Apply any advanced, non-cached settings from the user
        ApplyAdvancedOptions(finalOptions, options.SystemTextJsonOptions);

        return JsonSerializer.Serialize(source, finalOptions);
    }

    /// <summary>
    /// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
    /// </summary>
    public static T? FromJson<T>(this string? json, Action<JsonSerializerOptions>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        JsonSerializerOptions options;
        if (configureOptions == null)
        {
            // Optimized path: use the pre-cached default options which includes the flexible enum reader
            options = DeserializationOptions;
        }
        else
        {
            // Flexible path: create new options based on defaults and apply user configuration
            options = new JsonSerializerOptions(DeserializationOptions);
            configureOptions(options);
        }

        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// Deserializes a UTF-8 encoded byte array into an object of type <typeparamref name="T"/>.
    /// </summary>
    public static T? FromBytes<T>(this byte[]? bytes, Action<JsonSerializerOptions>? configureOptions = null)
    {
        if (bytes is null || bytes.Length == 0) return default;

        JsonSerializerOptions options;
        if (configureOptions == null)
        {
            options = DeserializationOptions;
        }
        else
        {
            options = new JsonSerializerOptions(DeserializationOptions);
            configureOptions(options);
        }

        return JsonSerializer.Deserialize<T>(bytes, options);
    }

    #region Obsolete API for Backward Compatibility

    /// <summary>
    /// Serializes any .NET object into a JSON string.
    /// </summary>
    [Obsolete("This method is obsolete and will be removed in a future version. Please use the overload that accepts an Action<JConvertOptions> for more flexibility.", false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string ToJson(
        this object? source,
        NamingConvention convention,
        ReferenceLoopHandling loopHandling = ReferenceLoopHandling.Throw,
        bool writeIndented = false)
    {
        return ToJson(source, options =>
        {
            options.NamingConvention = convention;
            options.LoopHandling = loopHandling;
            options.WriteIndented = writeIndented;
        });
    }

    #endregion

    #region Internals and Configuration

    // Cache keys now include EnumSerializationMode for both serializers
    private static readonly Dictionary<(NamingConvention, ReferenceLoopHandling, EnumSerializationMode), JsonSerializerOptions> SerializationOptionsCache = [];
    private static readonly Dictionary<(NamingConvention, ReferenceLoopHandling, bool, EnumSerializationMode), Newtonsoft.Json.JsonSerializerSettings> NewtonsoftSettingsCache = [];
    private static readonly JsonSerializerOptions DeserializationOptions;

    static JConvert()
    {
        // Populate System.Text.Json Serialization Cache for all possible combinations
        foreach (NamingConvention convention in Enum.GetValues<NamingConvention>())
        {
            foreach (ReferenceLoopHandling loopHandling in Enum.GetValues<ReferenceLoopHandling>())
            {
                foreach (EnumSerializationMode enumMode in Enum.GetValues<EnumSerializationMode>())
                {
                    bool serializeEnumAsString = enumMode == EnumSerializationMode.AsString;
                    var key = (convention, loopHandling, enumMode);
                    SerializationOptionsCache[key] = CreateSystemTextJsonOptions(convention, loopHandling, serializeEnumAsString);
                }
            }
        }

        // Configure Default Deserialization Options
        // This will always use the flexible enum reader. The 'serializeAsStrings' flag doesn't affect reading.
        DeserializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve,
        };

        var defaultConverters = GetDefaultConverters(serializeEnumsAsStrings: false); // 'false' is fine, reader is independent
        foreach (var converter in defaultConverters)
        {
            DeserializationOptions.Converters.Add(converter);
        }
    }

    private static IEnumerable<JsonConverter> GetDefaultConverters(bool serializeEnumsAsStrings)
    {
        yield return new FlexibleEnumConverterFactory(serializeEnumsAsStrings);
        yield return new RecursiveJTokenConverter();
        yield return new RecursiveJsonNodeConverter();
        yield return new RecursiveSpanJsonDynamicObjectConverter();
        yield return new ObjectToInferredTypesConverter();
    }

    private static JsonSerializerOptions CreateSystemTextJsonOptions(NamingConvention convention, ReferenceLoopHandling loopHandling, bool serializeEnumsAsStrings)
    {
        JsonNamingPolicy? policy = convention switch
        {
            NamingConvention.PascalCase => new PascalCaseNamingPolicy(),
            NamingConvention.CamelCase => JsonNamingPolicy.CamelCase,
            NamingConvention.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
            NamingConvention.KebabCaseLower => new KebabCaseLowerNamingPolicy(),
            _ => null,
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = policy,
            DictionaryKeyPolicy = policy,
            ReferenceHandler = loopHandling switch
            {
                ReferenceLoopHandling.Ignore => ReferenceHandler.IgnoreCycles,
                ReferenceLoopHandling.Preserve => ReferenceHandler.Preserve,
                _ => null,
            }
        };

        var converters = GetDefaultConverters(serializeEnumsAsStrings);
        foreach (var converter in converters)
        {
            options.Converters.Add(converter);
        }
        return options;
    }

    private static void ApplyAdvancedOptions(JsonSerializerOptions targetOptions, JsonSerializerOptions sourceOptions)
    {
        if (sourceOptions.DefaultBufferSize != 0) targetOptions.DefaultBufferSize = sourceOptions.DefaultBufferSize;
        if (sourceOptions.Encoder != null) targetOptions.Encoder = sourceOptions.Encoder;
        if (sourceOptions.NumberHandling != default) targetOptions.NumberHandling = sourceOptions.NumberHandling;
        if (sourceOptions.MaxDepth != 0) targetOptions.MaxDepth = sourceOptions.MaxDepth;

        var internalConverterTypes = new HashSet<Type>(targetOptions.Converters.Select(c => c.GetType()));
        foreach (var converter in sourceOptions.Converters)
        {
            if (!internalConverterTypes.Contains(converter.GetType()))
            {
                targetOptions.Converters.Add(converter);
            }
        }
    }

    private static Newtonsoft.Json.JsonSerializerSettings GetNewtonsoftCachedSettings(NamingConvention convention, ReferenceLoopHandling loopHandling, bool writeIndented, EnumSerializationMode enumSerialization)
    {
        var key = (convention, loopHandling, writeIndented, enumSerialization);
        if (NewtonsoftSettingsCache.TryGetValue(key, out var cached)) return cached;

        var settings = new Newtonsoft.Json.JsonSerializerSettings
        {
            Formatting = writeIndented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
            ReferenceLoopHandling = loopHandling switch
            {
                ReferenceLoopHandling.Ignore => Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                ReferenceLoopHandling.Preserve => Newtonsoft.Json.ReferenceLoopHandling.Serialize,
                _ => Newtonsoft.Json.ReferenceLoopHandling.Error,
            }
        };

        if (enumSerialization == EnumSerializationMode.AsString)
        {
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        }

        Newtonsoft.Json.Serialization.NamingStrategy? strategy = convention switch
        {
            NamingConvention.CamelCase => new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy(),
            NamingConvention.SnakeCaseLower => new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy(),
            NamingConvention.KebabCaseLower => new Newtonsoft.Json.Serialization.KebabCaseNamingStrategy(),
            _ => null,
        };

        if (strategy != null) settings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver { NamingStrategy = strategy };

        return NewtonsoftSettingsCache[key] = settings;
    }

    #endregion
}