using Linh.JsonKit.Enums;
using System.Text.Json;

namespace Linh.JsonKit;

/// <summary>
/// Provides configuration options for JConvert serialization operations.
/// </summary>
public sealed class JConvertOptions
{
    /// <summary>
    /// Gets or sets the naming convention for property and dictionary keys.
    /// Default is <see cref="NamingConvention.Unchanged"/>.
    /// </summary>
    public NamingConvention NamingConvention { get; set; } = NamingConvention.Unchanged;

    /// <summary>
    /// Gets or sets how reference loops are handled during serialization.
    /// Default is <see cref="ReferenceLoopHandling.Throw"/>.
    /// </summary>
    public ReferenceLoopHandling LoopHandling { get; set; } = ReferenceLoopHandling.Throw;

    /// <summary>
    /// Gets or sets a value that indicates whether the JSON output should be pretty-printed.
    /// Default is <c>false</c>.
    /// </summary>
    public bool WriteIndented { get; set; } = false;

    /// <summary>
    /// Provides direct access to the underlying System.Text.Json options for advanced customization.
    /// Settings here will be applied on top of the base configuration derived from other properties.
    /// </summary>
    public JsonSerializerOptions SystemTextJsonOptions { get; } = new();

    /// <summary>
    /// Gets or sets how enum values are serialized.
    /// Default is <see cref="EnumSerializationMode.AsNumber"/>.
    /// </summary>
    public EnumSerializationMode EnumSerialization { get; set; } = EnumSerializationMode.AsNumber;
}