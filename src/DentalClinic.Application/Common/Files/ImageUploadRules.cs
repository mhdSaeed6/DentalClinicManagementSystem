namespace DentalClinic.Application.Common.Files;

/// <summary>
/// Upload rules for clinical images. The content type is decided by sniffing
/// the file's magic bytes, never by the client's declared header or extension:
/// an attacker can rename a script to <c>.png</c>, but cannot forge the header
/// of a real image.
/// </summary>
public static class ImageUploadRules
{
    /// <summary>Hard cap enforced on every upload, independent of the client.</summary>
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    private static readonly byte[] s_pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Detects the image format from the first bytes of the file. Returns
    /// <see langword="false"/> for anything that is not JPEG, PNG or WebP.
    /// </summary>
    public static bool TryDetect(ReadOnlySpan<byte> header, out string contentType, out string extension)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            contentType = "image/jpeg";
            extension = ".jpg";
            return true;
        }

        if (header.Length >= 8 && header[..8].SequenceEqual(s_pngSignature))
        {
            contentType = "image/png";
            extension = ".png";
            return true;
        }

        // WebP is a RIFF container: "RIFF" + 4-byte length + "WEBP".
        if (header.Length >= 12 &&
            header[..4].SequenceEqual("RIFF"u8) &&
            header[8..12].SequenceEqual("WEBP"u8))
        {
            contentType = "image/webp";
            extension = ".webp";
            return true;
        }

        contentType = string.Empty;
        extension = string.Empty;
        return false;
    }

    /// <summary>
    /// Normalises the declared MIME type. <c>image/jpg</c> is a common alias for
    /// <c>image/jpeg</c>; the detected format is the source of truth downstream.
    /// </summary>
    public static string NormalizeContentType(string? contentType) =>
        contentType?.Trim().ToLowerInvariant() switch
        {
            "image/jpg" => "image/jpeg",
            { Length: > 0 } value => value,
            _ => string.Empty
        };
}