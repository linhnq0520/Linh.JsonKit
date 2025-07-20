using System.Text.Json;

namespace Linh.JsonKit.Enums;

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