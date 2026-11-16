namespace DentalClinic.Web.Components;

/// <summary>
/// Builds WhatsApp deep links (<c>https://wa.me</c>) from a stored phone
/// number. Purely client-side: the browser opens the link and the user sends
/// the prepared message — the app never delivers anything itself.
/// <para>
/// Kept free of UI concerns so the phone normalisation can be unit tested
/// (tests/DentalClinic.Web.Tests). The clinic's names and default country code
/// are passed in by the caller from the loaded clinic profile, never hardcoded
/// here: a second clinic must not send messages branded as the first one.
/// </para>
/// </summary>
public static class WhatsAppMessageBuilder
{
    private const string BaseUrl = "https://wa.me/";

    /// <summary>
    /// Normalises a stored phone number into the digits-only form wa.me
    /// expects (country code, no leading zeros).
    /// </summary>
    /// <param name="phone">The stored number, possibly formatted.</param>
    /// <param name="defaultCountryCode">
    /// The clinic's country code, e.g. <c>963</c>. When supplied, a number
    /// saved in local form (<c>0999…</c>) becomes <c>963999…</c>, which is the
    /// only way a local-format number can open a chat. Without it the digits are
    /// returned unchanged and wa.me decides whether they work.
    /// </param>
    /// <returns>Null when the number is unusable.</returns>
    public static string? TryNormalize(string? phone, string? defaultCountryCode = null)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string([.. phone.Where(char.IsDigit)]);

        // "00" is the international dialling prefix, not part of a country code.
        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        // A leading zero marks a local-format number. Drop it and prepend the
        // clinic's country code, otherwise the link points at a number that
        // cannot exist.
        if (digits.StartsWith('0') && NormalizeCountryCode(defaultCountryCode) is { } countryCode)
        {
            digits = countryCode + digits.TrimStart('0');
        }

        return digits.Length is < 7 or > 15 ? null : digits;
    }

    /// <summary>True when <see cref="TryNormalize"/> would produce a usable number.</summary>
    public static bool CanSend(string? phone, string? defaultCountryCode = null) =>
        TryNormalize(phone, defaultCountryCode) is not null;

    /// <summary>
    /// Builds the full wa.me URL, or null when the phone is unusable. The
    /// message body is URL-encoded so Arabic text and line breaks survive.
    /// </summary>
    public static string? TryBuildUrl(string? phone, string message, string? defaultCountryCode = null)
    {
        if (TryNormalize(phone, defaultCountryCode) is not { } digits)
        {
            return null;
        }

        return $"{BaseUrl}{digits}?text={Uri.EscapeDataString(message)}";
    }

    /// <summary>Prepared body for an appointment reminder.</summary>
    public static string AppointmentReminderMessage(
        bool arabic,
        string patientName,
        DateTime scheduledDateTime,
        int durationMinutes,
        string doctorName,
        string clinicName,
        string clinicNameArabic)
    {
        var when = ClinicFormatting.TimeRange(scheduledDateTime, durationMinutes);
        var date = ClinicFormatting.ShortDate(scheduledDateTime);
        var clinic = ClinicName(arabic, clinicName, clinicNameArabic);

        return arabic
            ? $"مرحباً {patientName}، نُذكّركم بموعدكم في {clinic} بتاريخ {date} الساعة {when} مع {(string.IsNullOrWhiteSpace(doctorName) ? "الطبيب" : $"د. {doctorName}")}. يرجى إبلاغنا في حال تعذّر الحضور."
            : $"Hello {patientName}, this is {clinic} reminding you of your appointment on {date} at {when} with {(string.IsNullOrWhiteSpace(doctorName) ? "Dr." : $"Dr. {doctorName}")}. Please let us know if you cannot attend.";
    }

    /// <summary>Prepared body for a general contact from the clinic (no appointment).</summary>
    public static string GeneralContactMessage(
        bool arabic,
        string patientName,
        string clinicName,
        string clinicNameArabic) => arabic
        ? $"مرحباً {patientName}، نود التواصل معكم بخصوص ملفكم في {ClinicName(arabic, clinicName, clinicNameArabic)}."
        : $"Hello {patientName}, we would like to talk to you about your file at {ClinicName(arabic, clinicName, clinicNameArabic)}.";

    /// <summary>
    /// The configured clinic name, or a neutral word when the profile has not
    /// loaded yet — a message must never carry an empty name.
    /// </summary>
    private static string ClinicName(bool arabic, string clinicName, string clinicNameArabic)
    {
        var configured = arabic ? clinicNameArabic : clinicName;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return arabic ? "العيادة" : "the clinic";
    }

    /// <summary>
    /// Reduces a configured country code to digits, rejecting anything that
    /// cannot be one (empty, leading zero, more than three digits).
    /// </summary>
    private static string? NormalizeCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        var digits = new string([.. countryCode.Where(char.IsDigit)]).TrimStart('0');
        return digits.Length is < 1 or > 3 ? null : digits;
    }
}
