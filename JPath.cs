using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Linh.JsonKit;

/// <summary>
/// Provides powerful and efficient utilities to query JSON structures using a path syntax.
/// Supports both <see cref="JsonElement"/> and <see cref="JsonNode"/>, and handles complex paths including indexing and wildcards.
/// </summary>
public static class JPath
{
    #region Public API for JsonNode

    /// <summary>
    /// Selects a single <see cref="JsonNode"/> using a JPath expression.
    /// </summary>
    /// <param name="node">The root <see cref="JsonNode"/> to query.</param>
    /// <param name="path">The JPath expression (e.g., "data.users[0].name").</param>
    /// <returns>The matching <see cref="JsonNode"/>, or null if the path does not match.</returns>
    public static JsonNode? SelectNode(this JsonNode? node, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (node == null) return null;

        var adapter = new JsonNodeAdapter(node);
        if (TrySelect(ref adapter, path.AsSpan(), out var result))
        {
            return result.Node;
        }
        return null;
    }

    /// <summary>
    /// Attempts to get a strongly-typed value from a <see cref="JsonNode"/> using a JPath expression.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize the value into.</typeparam>
    /// <param name="node">The root <see cref="JsonNode"/> to query.</param>
    /// <param name="path">The JPath expression.</param>
    /// <param name="result">When this method returns, contains the deserialized value if the query was successful; otherwise, the default value of T.</param>
    /// <param name="options">Optional <see cref="JsonSerializerOptions"/> for deserialization.</param>
    /// <returns><c>true</c> if the path was found and the value could be converted to the target type; otherwise, <c>false</c>.</returns>
    public static bool TryGetValueByPath<T>(this JsonNode? node, string path, [MaybeNullWhen(false)] out T result, JsonSerializerOptions? options = null)
    {
        var foundNode = SelectNode(node, path);
        if (foundNode == null)
        {
            result = default;
            return false;
        }
        return TryConvertJsonNode(foundNode, out result, options);
    }

    #endregion

    #region Public API for JsonElement

    /// <summary>
    /// Selects a single <see cref="JsonElement"/> using a JPath expression.
    /// </summary>
    /// <param name="element">The root <see cref="JsonElement"/> to query.</param>
    /// <param name="path">The JPath expression (e.g., "data.users[0].name").</param>
    /// <returns>A <see cref="JsonElement"/> if the path matches; otherwise, null.</returns>
    public static JsonElement? SelectElement(this JsonElement element, string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var adapter = new JsonElementAdapter(element);
        if (TrySelect(ref adapter, path.AsSpan(), out var result))
        {
            return result.Element;
        }
        return null;
    }

    /// <summary>
    /// Attempts to get a strongly-typed value from a <see cref="JsonElement"/> using a JPath expression.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize the value into.</typeparam>
    /// <param name="element">The root <see cref="JsonElement"/> to query.</param>
    /// <param name="path">The JPath expression.</param>
    /// <param name="result">When this method returns, contains the deserialized value if the query was successful; otherwise, the default value of T.</param>
    /// <param name="options">Optional <see cref="JsonSerializerOptions"/> for deserialization.</param>
    /// <returns><c>true</c> if the path was found and the value could be converted to the target type; otherwise, <c>false</c>.</returns>
    public static bool TryGetValueByPath<T>(this JsonElement element, string path, [MaybeNullWhen(false)] out T result, JsonSerializerOptions? options = null)
    {
        var foundElement = SelectElement(element, path);
        if (!foundElement.HasValue)
        {
            result = default;
            return false;
        }
        return TryConvertJsonElement(foundElement.Value, out result, options);
    }

    #endregion

    #region Core Logic

    // An interface to abstract away the differences between JsonElement and JsonNode
    private interface IJsonWrapper<T> where T : IJsonWrapper<T>
    {
        JsonValueKind ValueKind { get; }
        bool TryGetProperty(ReadOnlySpan<char> propertyName, out T result);
        bool TryGetElementAt(int index, out T result);
        JsonElement Element { get; }
        JsonNode? Node { get; }
    }

    // Core selection logic that works with any IJsonWrapper
    private static bool TrySelect<T>(ref T current, ReadOnlySpan<char> path, out T result) where T : IJsonWrapper<T>
    {
        result = current;
        var reader = new PathReader(path);

        while (reader.MoveNext())
        {
            var segment = reader.Current;
            if (segment.IsProperty)
            {
                if (!result.TryGetProperty(segment.Property, out result)) return false;
            }
            else // Is Index
            {
                if (!result.TryGetElementAt(segment.ArrayIndex, out result)) return false;
            }
        }
        return true;
    }

    #endregion

    #region Conversion Helpers

    private static bool TryConvertJsonNode<T>(JsonNode node, [MaybeNullWhen(false)] out T result, JsonSerializerOptions? options)
    {
        result = default;
        try
        {
            // Direct cast for JsonNode types themselves
            if (node is T typedNode)
            {
                result = typedNode;
                return true;
            }

            // For JsonValue, try its own optimized TryGetValue
            if (node is JsonValue val && val.TryGetValue(out result))
            {
                return true;
            }

            // Fallback to deserialization
            result = node.Deserialize<T>(options);
            return result != null || (node as JsonValue)?.GetValue<object>() == null; // Handle explicit null
        }
        catch (JsonException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static bool TryConvertJsonElement<T>(JsonElement element, [MaybeNullWhen(false)] out T result, JsonSerializerOptions? options)
    {
        result = default;
        if (element.ValueKind == JsonValueKind.Null)
        {
            // Null is a valid value for reference types or Nullable<T>
            return default(T) == null;
        }

        try
        {
            // For simple types, use the built-in efficient getters
            var targetType = typeof(T);
            if (targetType == typeof(string)) result = (T)(object)element.GetString()!;
            else if (targetType == typeof(int)) result = (T)(object)element.GetInt32();
            else if (targetType == typeof(long)) result = (T)(object)element.GetInt64();
            else if (targetType == typeof(double)) result = (T)(object)element.GetDouble();
            else if (targetType == typeof(decimal)) result = (T)(object)element.GetDecimal();
            else if (targetType == typeof(bool)) result = (T)(object)element.GetBoolean();
            else if (targetType == typeof(DateTime)) result = (T)(object)element.GetDateTime();
            else if (targetType == typeof(Guid)) result = (T)(object)element.GetGuid();
            else if (targetType == typeof(JsonElement)) result = (T)(object)element;
            else
            {
                // For complex types, fallback to deserialization
                result = element.Deserialize<T>(options);
                return result != null;
            }
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
        catch (JsonException) { return false; }
    }

    #endregion

    #region Private Structs for Abstraction and Parsing

    // Adapter for JsonElement
    private readonly struct JsonElementAdapter(JsonElement element) : IJsonWrapper<JsonElementAdapter>
    {
        public JsonElement Element { get; } = element;
        public JsonValueKind ValueKind => Element.ValueKind;
        public JsonNode? Node => null;

        public bool TryGetProperty(ReadOnlySpan<char> propertyName, out JsonElementAdapter result)
        {
            if (ValueKind == JsonValueKind.Object && Element.TryGetProperty(propertyName, out var nextElement))
            {
                result = new JsonElementAdapter(nextElement);
                return true;
            }
            result = default;
            return false;
        }

        public bool TryGetElementAt(int index, out JsonElementAdapter result)
        {
            if (ValueKind == JsonValueKind.Array)
            {
                int length = Element.GetArrayLength();
                int finalIndex = index >= 0 ? index : length + index;
                if (finalIndex >= 0 && finalIndex < length)
                {
                    result = new JsonElementAdapter(Element.EnumerateArray().ElementAt(finalIndex));
                    return true;
                }
            }
            result = default;
            return false;
        }
    }

    // Adapter for JsonNode
    private readonly struct JsonNodeAdapter(JsonNode? node) : IJsonWrapper<JsonNodeAdapter>
    {
        public JsonNode? Node { get; } = node;
        public JsonValueKind ValueKind => Node?.GetValue<JsonElement>().ValueKind ?? JsonValueKind.Undefined;
        public JsonElement Element => Node?.GetValue<JsonElement>() ?? default;

        public bool TryGetProperty(ReadOnlySpan<char> propertyName, out JsonNodeAdapter result)
        {
            if (Node is JsonObject obj && obj.TryGetPropertyValue(propertyName.ToString(), out var nextNode))
            {
                result = new JsonNodeAdapter(nextNode);
                return true;
            }
            result = default;
            return false;
        }

        public bool TryGetElementAt(int index, out JsonNodeAdapter result)
        {
            if (Node is JsonArray arr)
            {
                int count = arr.Count;
                int finalIndex = index >= 0 ? index : count + index;
                if (finalIndex >= 0 && finalIndex < count)
                {
                    result = new JsonNodeAdapter(arr[finalIndex]);
                    return true;
                }
            }
            result = default;
            return false;
        }
    }

    // Zero-allocation path parser
    private ref struct PathReader
    {
        private ReadOnlySpan<char> _path;
        public PathSegment Current { get; private set; }

        public PathReader(ReadOnlySpan<char> path)
        {
            _path = path;
            Current = default;
        }

        public bool MoveNext()
        {
            if (_path.IsEmpty) return false;

            if (_path[0] == '.') _path = _path.Slice(1);
            if (_path.IsEmpty) return false;

            if (_path[0] == '[')
            {
                _path = _path.Slice(1); // Skip '['
                int endBracket = _path.IndexOf(']');
                if (endBracket == -1) throw new JsonException("Invalid JPath: Unmatched '['.");

                var indexSpan = _path.Slice(0, endBracket);
                if (!int.TryParse(indexSpan, out int index))
                {
                    throw new JsonException($"Invalid JPath: Cannot parse index '{indexSpan.ToString()}'.");
                }
                Current = new PathSegment(index);
                _path = _path.Slice(endBracket + 1);
                return true;
            }
            else
            {
                int start = 0;
                while (start < _path.Length && _path[start] == '\\') start += 2; // Skip escaped chars at start

                int segmentEnd = -1;
                for (int i = start; i < _path.Length; i++)
                {
                    if (_path[i] == '.' || _path[i] == '[')
                    {
                        if (i > 0 && _path[i - 1] != '\\')
                        {
                            segmentEnd = i;
                            break;
                        }
                    }
                }

                var propertySpan = segmentEnd == -1 ? _path : _path.Slice(0, segmentEnd);
                if (propertySpan.IsEmpty) return false;

                Current = new PathSegment(propertySpan);
                _path = segmentEnd == -1 ? ReadOnlySpan<char>.Empty : _path.Slice(segmentEnd);
                return true;
            }
        }
    }

    // Represents a segment of a path (either a property or an array index)
    private readonly ref struct PathSegment
    {
        public readonly ReadOnlySpan<char> Property;
        public readonly int ArrayIndex;
        public readonly bool IsProperty;

        // Constructor for property segment (zero-allocation)
        public PathSegment(ReadOnlySpan<char> property)
        {
            Property = property;
            IsProperty = true;
            ArrayIndex = 0;
        }

        // Constructor for index segment
        public PathSegment(int index)
        {
            ArrayIndex = index;
            IsProperty = false;
            Property = default;
        }
    }

    #endregion
}