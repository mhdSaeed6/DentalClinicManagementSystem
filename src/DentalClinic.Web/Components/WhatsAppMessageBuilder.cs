namespace DentalClinic.Web.Components;

/// <summary>
/// Builds WhatsApp deep links (<c>https://wa.me</c>) from a stored phone
/// number. Purely client-side: the browser opens the link and the user sends
/// the prepared message — the app never delivers anything itself.
/// <para>
/// Kept free of UI concerns so the phone normalisation can be unit tested
/// (tests/DentalClinic.Web.Tests). The clinic name is the same hardcoded
/// branding used elsewhere in the UI until a clinic-settings entity exists.
/// </para>
/// </summary>
public static class WhatsAppMessageBuilder
{
    private const string BaseUrl = "https://wa.me/";

    private const string ClinicName = "Al Noor Dental";
    private const string ClinicNameArabic = "عيادة النور";

    /// <summary>
    /// Normalises a stored phone number into the digits-only form wa.me
    /// expects (country code, no leading zeros). Returns null when the number
    /// is unusable — non-digit characters other than a leading + / 00 prefix,
    /// or fewer than 7 / more than 15 digits. Numbers saved without a country
    /// code (e.g. "09…") cannot be fixed here; wa.me decides whether they work.
    /// </summary>
    public static string? TryNormalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string([.. phone.Where(char.IsDigit)]);

        if (digits.Length is < 7 or > 15)
        {
            return null;
        }

        return digits.StartsWith("00", StringComparison.Ordinal)
            ? digits[2..]
            : digits;
    }

    /// <summary>True when <see cref="TryNormalize"/> would produce a usable number.</summary>
    public static bool CanSend(string? phone) => TryNormalize(phone) is not null;

    /// <summary>
    /// Builds the full wa.me URL, or null when the phone is unusable. The
    /// message body is URL-encoded so Arabic text and line breaks survive.
    /// </summary>
    public static string? TryBuildUrl(string? phone, string message)
    {
        if (TryNormalize(phone) is not { } digits)
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
        string doctorName)
    {
        var when = ClinicFormatting.TimeRange(scheduledDateTime, durationMinutes);
        var date = ClinicFormatting.ShortDate(scheduledDateTime);

        return arabic
            ? $"مرحباً {patientName}، نُذكّركم بموعدكم في {ClinicNameArabic} بتاريخ {date} الساعة {when} مع {(string.IsNullOrWhiteSpace(doctorName) ? "الطبيب" : $"د. {doctorName}")}. يرجى إبلاغنا في حال تعذّر الحضور."
            : $"Hello {patientName}, this is {ClinicName} reminding you of your appointment on {date} at {when} with {(string.IsNullOrWhiteSpace(doctorName) ? "Dr." : $"Dr. {doctorName}")}. Please let us know if you cannot attend.";
    }

    /// <summary>Prepared body for a general contact from the clinic (no appointment).</summary>
    public static string GeneralContactMessage(bool arabic, string patientName) => arabic
        ? $"مرحباً {patientName}، نود التواصل معكم بخصوص ملفكم في {ClinicNameArabic}."
        : $"Hello {patientName}, we would like to talk to you about your file at {ClinicName}.";
}
