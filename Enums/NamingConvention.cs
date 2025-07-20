namespace Linh.JsonKit.Enums;

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