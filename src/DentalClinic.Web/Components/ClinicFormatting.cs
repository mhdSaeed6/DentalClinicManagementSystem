using System.Globalization;

using DentalClinic.Web.Api;

namespace DentalClinic.Web.Components;

public static class ClinicFormatting
{
    public const string Currency = "USD";

    public static string Money(decimal amount)
    {
        var decimals = Currency.Equals("USD", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
        return amount.ToString($"N{decimals}", CultureInfo.InvariantCulture) + " " + Currency;
    }

    public static string ShortDate(DateTime date) =>
        date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    public static string LongDate(DateTime date) =>
        date.ToString("ddd, dd MMM yyyy", CultureInfo.InvariantCulture);

    public static string DateTimeLabel(DateTime date) =>
        date.ToString("dd MMM yyyy · HH:mm", CultureInfo.InvariantCulture);

    public static string TimeLabel(DateTime date) =>
        date.ToString("HH:mm", CultureInfo.InvariantCulture);

    public static string TimeRange(DateTime start, int durationMinutes)
    {
        var end = start.AddMinutes(durationMinutes);
        return string.Create(CultureInfo.InvariantCulture, $"{start:HH:mm} – {end:HH:mm}");
    }

    public static string Initials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return "DC";
        }

        var initials = parts.Length == 1
            ? parts[0][..1]
            : string.Concat(parts[0][..1], parts[^1][..1]);

        return initials.ToUpperInvariant();
    }

    public static int Age(DateTime dateOfBirth)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - dateOfBirth.Date.Year;

        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }

        return Math.Max(age, 0);
    }

    public static string FullName(string? first, string? last) =>
        $"{first} {last}".Trim();

    // ------------------------------------------------------------- Labels

    public static string GenderLabel(int gender, bool arabic) => gender == 1
        ? (arabic ? "أنثى" : "Female")
        : (arabic ? "ذكر" : "Male");

    public static string GenderValue(string? gender) => gender ?? "Male";

    public static string StatusLabel(AppointmentStatus status, bool arabic) => status switch
    {
        AppointmentStatus.Scheduled => arabic ? "محجوز" : "Scheduled",
        AppointmentStatus.Completed => arabic ? "مكتمل" : "Completed",
        AppointmentStatus.Cancelled => arabic ? "ملغى" : "Cancelled",
        AppointmentStatus.NoShow => arabic ? "لم يحضر" : "No-show",
        _ => arabic ? "غير معروف" : "Unknown",
    };

    public static string StatusCss(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Scheduled => "info",
        AppointmentStatus.Completed => "success",
        AppointmentStatus.Cancelled => "muted",
        AppointmentStatus.NoShow => "warning",
        _ => "muted",
    };

    public static string PaymentLabel(PaymentStatus status, bool arabic) => status switch
    {
        PaymentStatus.Unpaid => arabic ? "غير مدفوعة" : "Unpaid",
        PaymentStatus.PartiallyPaid => arabic ? "مدفوعة جزئياً" : "Partially paid",
        PaymentStatus.Paid => arabic ? "مدفوعة" : "Paid",
        _ => arabic ? "غير معروف" : "Unknown",
    };

    /// <summary>
    /// Label for the pharmacy-free <c>MedicalAlertSeverity</c> value the API
    /// computes on <c>Patient.MedicalHistory</c> (0 = none, 1 = warning, 2 = critical).
    /// </summary>
    public static string AlertLabel(int severity, bool arabic) => severity switch
    {
        2 => arabic ? "حرج — استشر الطبيب قبل العلاج" : "Critical — check before treatment",
        1 => arabic ? "يتطلب انتباهاً" : "Attention needed",
        _ => arabic ? "سليم" : "Clear",
    };

    public static string AlertCss(int severity) => severity switch
    {
        2 => "danger",
        1 => "warning",
        _ => "success",
    };

    /// <summary>
    /// One-line summary of the flags that drive the alert (empty string when
    /// there are none) — used by list rows and the appointment drawer so the
    /// "why" behind the badge is visible at a glance.
    /// </summary>
    public static string MedicalFlagsSummary(MedicalHistoryDto? history, bool arabic)
    {
        if (history is null)
        {
            return string.Empty;
        }

        var flags = new List<string>();

        if (history.HasBleedingDisorder)
        {
            flags.Add(arabic ? "اضطراب نزيف" : "Bleeding disorder");
        }

        if (history.HasDiabetes)
        {
            flags.Add(arabic ? "سكري" : "Diabetes");
        }

        if (history.HasHypertension)
        {
            flags.Add(arabic ? "ضغط مرتفع" : "Hypertension");
        }

        if (history.HasHeartDisease)
        {
            flags.Add(arabic ? "أمراض قلب" : "Heart disease");
        }

        if (history.HasEpilepsy)
        {
            flags.Add(arabic ? "صرع" : "Epilepsy");
        }

        if (history.HasAsthma)
        {
            flags.Add(arabic ? "ربو" : "Asthma");
        }

        if (history.IsPregnant)
        {
            flags.Add(history.PregnancyWeeks is { } weeks
                ? $"{(arabic ? "حمل" : "Pregnancy")} ({(arabic ? "الأسبوع " : "week ")}{weeks})"
                : arabic ? "حمل" : "Pregnancy");
        }

        if (!string.IsNullOrWhiteSpace(history.BloodPressureReading))
        {
            flags.Add($"{(arabic ? "ضغط" : "BP")} {history.BloodPressureReading}");
        }

        if (!string.IsNullOrWhiteSpace(history.Allergies))
        {
            flags.Add($"{(arabic ? "حساسية" : "Allergies")}: {history.Allergies}");
        }

        return string.Join(" · ", flags);
    }

    public static string PaymentCss(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "danger",
        PaymentStatus.PartiallyPaid => "warning",
        PaymentStatus.Paid => "success",
        _ => "muted",
    };

    /// <summary>
    /// Lifecycle (Draft/Issued) is a separate concern from payment status, so it
    /// gets its own label and colour.
    /// </summary>
    public static string LifecycleLabel(InvoiceLifecycleStatus status, bool arabic) => status switch
    {
        InvoiceLifecycleStatus.Draft => arabic ? "مسودة" : "Draft",
        InvoiceLifecycleStatus.Issued => arabic ? "صادرة" : "Issued",
        _ => arabic ? "غير معروف" : "Unknown",
    };

    public static string LifecycleCss(InvoiceLifecycleStatus status) => status switch
    {
        InvoiceLifecycleStatus.Draft => "warning",
        InvoiceLifecycleStatus.Issued => "info",
        _ => "muted",
    };

    /// <summary>Friendly FDI tooth label, e.g. "UR 1" style segmentation.</summary>
    public static string ToothLabel(int toothNumber)
    {
        if (toothNumber is < 11 or > 85)
        {
            return toothNumber.ToString(CultureInfo.InvariantCulture);
        }

        var quadrant = toothNumber / 10;
        var position = toothNumber % 10;

        var region = quadrant switch
        {
            1 => "UR",
            2 => "UL",
            3 => "LL",
            4 => "LR",
            5 => "UR",
            6 => "UL",
            7 => "LL",
            8 => "LR",
            _ => "??",
        };

        return $"{region}·{position}";
    }

    /// <summary>
    /// Null-tolerant overload (S8): a treatment may be recorded without a
    /// specific tooth, and an em dash keeps the badge column visually aligned
    /// without pretending a tooth was chosen.
    /// </summary>
    public static string ToothLabel(int? toothNumber) => toothNumber is null
        ? "—"
        : ToothLabel(toothNumber.Value);

    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:N1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):N1} MB",
    };
}
