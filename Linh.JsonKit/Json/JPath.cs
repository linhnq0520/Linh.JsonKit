using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Linh.JsonKit.Json
{
    /// <summary>
    /// Provides utilities to access deeply nested values within JSON structures using dot/bracket notation paths.
    /// Supports both <see cref="JsonElement"/> and <see cref="JsonNode"/> representations.
    /// </summary>
    public static class JPath
    {
        /// <summary>
        /// Attempts to get a strongly typed value from a <see cref="JsonElement"/> using a JSON path.
        /// </summary>
        /// <typeparam name="T">The expected type of the value (e.g., string, int, bool, custom types).</typeparam>
        /// <param name="element">The input <see cref="JsonElement"/> to query.</param>
        /// <param name="path">The path to the desired value, e.g., "user.address[0].street". Supports escaped dots (\.) and brackets (\[ \]).</param>
        /// <param name="result">The output variable to store the result if successful.</param>
        /// <param name="options">Optional <see cref="JsonSerializerOptions"/> for deserialization of complex types.</param>
        /// <returns>True if the value is found and can be converted to <typeparamref name="T"/>, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        public static bool TryGetValueByPath<T>(
            in JsonElement element,
            string path,
            out T? result,
            JsonSerializerOptions? options = null
        )
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            result = default;

            if (element.ValueKind == JsonValueKind.Undefined || path.Length == 0)
                return false;

            var current = element;
            var reader = new PathReader(path.AsSpan());

            while (reader.MoveNext())
            {
                var segment = reader.Current;

                if (segment.IsProperty)
                {
                    if (
                        current.ValueKind != JsonValueKind.Object
                        || !current.TryGetProperty(segment.Property, out current)
                    )
                        return false;
                }
                else
                {
                    if (current.ValueKind != JsonValueKind.Array)
                        return false;

                    int index =
                        segment.ArrayIndex >= 0
                            ? segment.ArrayIndex
                            : current.GetArrayLength() + segment.ArrayIndex;

                    if (index < 0 || index >= current.GetArrayLength())
                        return false;

                    current = current.EnumerateArray().ElementAt(index);
                }
            }

            return TryConvertJsonElement(current, out result, options);
        }

        /// <summary>
        /// Attempts to get a strongly typed value from a <see cref="JsonNode"/> using a JSON path.
        /// </summary>
        /// <typeparam name="T">The expected type of the value (e.g., string, int, bool, custom types).</typeparam>
        /// <param name="node">The input <see cref="JsonNode"/> to query.</param>
        /// <param name="path">The path to the desired value, e.g., "items[2].name". Supports escaped dots (\.) and brackets (\[ \]).</param>
        /// <param name="result">The output variable to store the result if successful.</param>
        /// <param name="options">Optional <see cref="JsonSerializerOptions"/> for deserialization of complex types.</param>
        /// <returns>True if the value is found and can be converted to <typeparamref name="T"/>, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
        public static bool TryGetValueByPath<T>(
            this JsonNode? node,
            string path,
            out T? result,
            JsonSerializerOptions? options = null
        )
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            result = default;

            if (node == null || path.Length == 0)
                return false;

            var current = node;
            var reader = new PathReader(path.AsSpan());

            while (reader.MoveNext())
            {
                var segment = reader.Current;

                if (segment.IsProperty)
                {
                    if (
                        current is not JsonObject obj
                        || !obj.TryGetPropertyValue(segment.Property, out current)
                    )
                        return false;
                }
                else
                {
                    if (current is not JsonArray arr)
                        return false;

                    int index =
                        segment.ArrayIndex >= 0
                            ? segment.ArrayIndex
                            : arr.Count + segment.ArrayIndex;

                    if (index < 0 || index >= arr.Count)
                        return false;

                    current = arr[index];
                }
            }

            return TryConvertJsonNode(current, out result, options);
        }

        private static bool TryConvertJsonElement<T>(
            JsonElement element,
            out T? result,
            JsonSerializerOptions? options
        )
        {
            result = default;
            try
            {
                if (element.ValueKind == JsonValueKind.Null)
                    return !typeof(T).IsValueType;

                Type targetType = typeof(T);
                if (targetType == typeof(string))
                {
                    var @string = element.GetString();
                    if (!string.IsNullOrWhiteSpace(@string))
                    {
                        result = (T)(object)@string;
                    }
                }
                else if (targetType == typeof(int))
                    result = (T)(object)element.GetInt32();
                else if (targetType == typeof(long))
                    result = (T)(object)element.GetInt64();
                else if (targetType == typeof(double))
                    result = (T)(object)element.GetDouble();
                else if (targetType == typeof(decimal))
                    result = (T)(object)element.GetDecimal();
                else if (targetType == typeof(bool))
                    result = (T)(object)element.GetBoolean();
                else if (targetType == typeof(DateTime))
                    result = (T)(object)element.GetDateTime();
                else if (targetType == typeof(JsonElement))
                    result = (T)(object)element;
                else
                {
                    result = element.Deserialize<T>(options);
                    return result != null;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvertJsonNode<T>(
            JsonNode? node,
            out T? result,
            JsonSerializerOptions? options
        )
        {
            result = default;
            if (node == null)
                return !typeof(T).IsValueType;

            try
            {
                if (node is JsonValue val && val.TryGetValue<T>(out var value))
                {
                    result = value;
                    return true;
                }
                else if (node is T casted)
                {
                    result = casted;
                    return true;
                }
                else
                {
                    result = node.Deserialize<T>(options);
                    return result != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private ref struct PathReader
        {
            private ReadOnlySpan<char> _path;
            private int _pos;
            public PathSegment Current { get; private set; }

            public PathReader(ReadOnlySpan<char> path)
            {
                _path = path;
                _pos = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (_pos >= _path.Length)
                    return false;

                if (_path[_pos] == '.')
                    _pos++;

                if (_pos >= _path.Length)
                    return false;

                if (_path[_pos] == '[')
                {
                    _pos++;
                    int indexStart = _pos;
                    bool isNegative = false;
                    if (_pos < _path.Length && _path[_pos] == '-')
                    {
                        isNegative = true;
                        _pos++;
                    }
                    while (_pos < _path.Length && char.IsDigit(_path[_pos]))
                        _pos++;

                    if (_pos >= _path.Length || _path[_pos] != ']')
                        return false;

                    int index = int.Parse(_path.Slice(indexStart, _pos - indexStart));
                    if (isNegative)
                        index = -index;
                    _pos++;

                    Current = new PathSegment(index);
                    return true;
                }
                else
                {
                    int start = _pos;
                    var sb = new StringBuilder();
                    while (_pos < _path.Length && _path[_pos] != '.' && _path[_pos] != '[')
                    {
                        if (_path[_pos] == '\\' && _pos + 1 < _path.Length)
                        {
                            sb.Append(_path[_pos + 1]);
                            _pos += 2;
                        }
                        else
                        {
                            sb.Append(_path[_pos]);
                            _pos++;
                        }
                    }

                    if (sb.Length == 0)
                        return false;

                    Current = new PathSegment(sb.ToString());
                    return true;
                }
            }
        }

        private readonly struct PathSegment
        {
            public string Property { get; }
            public int ArrayIndex { get; }
            public bool IsProperty { get; }

            public PathSegment(string property)
            {
                Property = property;
                ArrayIndex = -1;
                IsProperty = true;
            }

            public PathSegment(int index)
            {
                ArrayIndex = index;
                Property = string.Empty;
                IsProperty = false;
            }
        }
    }
}
