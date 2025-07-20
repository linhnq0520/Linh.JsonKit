// FILENAME: Linh.JsonKit/Enums/EnumSerializationMode.cs
namespace Linh.JsonKit.Enums;

/// <summary>
/// Specifies how enum values should be serialized.
/// </summary>
public enum EnumSerializationMode
{
    /// <summary>
    /// Serializes enums as their underlying numeric value (e.g., 0, 1, 2). This is the default behavior.
    /// </summary>
    AsNumber,

    /// <summary>
    /// Serializes enums as their string name (e.g., "Active", "Inactive").
    /// </summary>
    AsString
}