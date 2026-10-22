using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DentalClinic.Application.Common.Logging;

/// <summary>
/// Renders a request object for logging without ever exposing a secret.
/// <para>
/// Two independent rules are applied so a credential cannot slip through by
/// renaming a property: the <em>name</em> of a property decides whether its
/// value is secret (password, token, secret, …), and the <em>shape</em> of a
/// string decides as well (a JWT or an opaque base64 credential is masked even
/// when it arrives under an innocent name).
/// </para>
/// </summary>
public static class SensitiveDataRedactor
{
    /// <summary>
    /// Placeholder written in place of a redacted value.
    /// </summary>
    public const string RedactedValue = "***REDACTED***";

    /// <summary>
    /// How deep the redactor walks into nested objects before it stops.
    /// </summary>
    private const int MaxDepth = 3;

    /// <summary>
    /// A value that is at least this long and looks like random base64 is
    /// treated as an opaque credential (a refresh token, a raw API key, …).
    /// </summary>
    private const int OpaqueCredentialMinLength = 32;

    private static readonly string[] SensitiveNameFragments =
    [
        "password",
        "passwd",
        "pwd",
        "token",
        "secret",
        "credential",
        "authorization",
        "apikey",
        "api_key",
        "signature",
    ];

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false,
    };

    /// <summary>
    /// Produces a log-safe, JSON representation of <paramref name="request"/>.
    /// </summary>
    public static string Describe(object? request)
    {
        if (request is null)
        {
            return "{}";
        }

        var redacted = Redact(request, 0);

        return JsonSerializer.Serialize(redacted, SerializerOptions);
    }

    /// <summary>
    /// Whether a property name denotes a secret.
    /// </summary>
    public static bool IsSensitiveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return SensitiveNameFragments.Any(fragment =>
            name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Whether a string value has the shape of a credential even when the
    /// property it arrived under is not obviously sensitive.
    /// </summary>
    public static bool IsSecretValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return LooksLikeJwt(value) || LooksLikeOpaqueCredential(value);
    }

    private static object? Redact(object? value, int depth)
    {
        switch (value)
        {
            case null:
                return null;
            case string text:
                return IsSecretValue(text) ? RedactedValue : text;
            case bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                return value;
            case Guid or DateTime or DateTimeOffset or TimeSpan or DateOnly or TimeOnly:
                return value;
            case Enum enumeration:
                return enumeration.ToString();
        }

        if (depth >= MaxDepth)
        {
            return "…";
        }

        if (value is IDictionary dictionary)
        {
            var map = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                map[key] = IsSensitiveName(key) ? RedactedValue : Redact(entry.Value, depth + 1);
            }

            return map;
        }

        if (value is IEnumerable sequence)
        {
            var items = new List<object?>();

            foreach (var item in sequence)
            {
                items.Add(Redact(item, depth + 1));
            }

            return items;
        }

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? propertyValue;

            try
            {
                propertyValue = property.GetValue(value);
            }
            catch (Exception)
            {
                // A property that throws must never break logging.
                propertyValue = null;
            }

            properties[property.Name] =
                IsSensitiveName(property.Name) ? RedactedValue : Redact(propertyValue, depth + 1);
        }

        return properties;
    }

    private static bool LooksLikeJwt(string value)
    {
        var parts = value.Split('.');

        if (parts.Length != 3 || parts[0].Length < 10)
        {
            return false;
        }

        return parts.All(part => part.Length > 0 && part.All(IsBase64UrlCharacter));
    }

    private static bool LooksLikeOpaqueCredential(string value)
    {
        if (value.Length < OpaqueCredentialMinLength ||
            value.Any(char.IsWhiteSpace) ||
            Guid.TryParse(value, out _))
        {
            return false;
        }

        if (!value.All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_'))
        {
            return false;
        }

        return value.Any(char.IsAsciiDigit) && value.Any(char.IsAsciiLetterUpper);
    }

    private static bool IsBase64UrlCharacter(char c) =>
        char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '=';
}
