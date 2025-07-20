using System.Text.Json;

namespace Linh.JsonKit.Policies;

/// <summary>
/// Converts a name to PascalCase (e.g., "propertyName" becomes "PropertyName").
/// Internal implementation detail for JConvert.
/// </summary>
internal sealed class PascalCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        if (name.Length == 1)
            return name.ToUpperInvariant();

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}