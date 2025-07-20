using Linh.JsonKit.Json.Resolver;
using SpanJson;

namespace Linh.JsonKit;

public static partial class JConvert
{
    /// <summary>
    /// Serializes an object to a JSON string using UTF-16 encoding with the default SpanJsonResolver.
    /// </summary>
    public static string ToJsonUtf16<T>(this T obj) => JsonSerializer.Generic.Utf16.Serialize<T, SpanJsonResolver<char>>(obj);

    /// <summary>
    /// Serializes an object to a JSON byte array using UTF-8 encoding with the default SpanJsonResolver.
    /// </summary>
    public static byte[] ToJsonUtf8Bytes<T>(this T obj) => JsonSerializer.Generic.Utf8.Serialize<T, SpanJsonResolver<byte>>(obj);

    /// <summary>
    /// Deserializes a JSON string (UTF-16) into an object of type <typeparamref name="T"/> using the default SpanJsonResolver.
    /// </summary>
    public static T FromJsonUtf16<T>(this string json) => JsonSerializer.Generic.Utf16.Deserialize<T, SpanJsonResolver<char>>(json);

    /// <summary>
    /// Deserializes a UTF-8 encoded JSON byte array into an object of type <typeparamref name="T"/> using the default SpanJsonResolver.
    /// </summary>
    public static T FromJsonUtf8<T>(this byte[] jsonBytes) => JsonSerializer.Generic.Utf8.Deserialize<T, SpanJsonResolver<byte>>(jsonBytes);
}