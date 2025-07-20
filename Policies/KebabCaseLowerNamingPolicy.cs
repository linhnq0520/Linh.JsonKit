using System.Text;
using System.Text.Json;

namespace Linh.JsonKit.Policies;

/// <summary>
/// Converts a name to kebab-case (e.g., "PropertyName" becomes "property-name").
/// Internal implementation detail for JConvert.
/// </summary>
internal sealed class KebabCaseLowerNamingPolicy : JsonNamingPolicy
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