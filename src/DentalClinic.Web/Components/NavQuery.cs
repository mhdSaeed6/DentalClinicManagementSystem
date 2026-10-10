namespace DentalClinic.Web.Components;

/// <summary>Tiny helpers for reading query string values from the current URI.</summary>
public static class NavQuery
{
    public static Dictionary<string, string> Parse(string uri)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = uri.IndexOf('?', StringComparison.Ordinal);

        if (index < 0)
        {
            return result;
        }

        var query = uri[(index + 1)..];

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
