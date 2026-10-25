namespace DentalClinic.Domain.Patients;

/// <summary>
/// Single source of truth for turning a <see cref="MedicalHistory"/> into a
/// <see cref="MedicalAlertSeverity"/> (AUDIT.md §6.8). Rules are deliberately
/// simple and conservative; the thresholds should be confirmed with the
/// clinician before being treated as medical guidance.
/// </summary>
public static class MedicalAlertEvaluator
{
    // Blood-pressure thresholds. Systolic at or above 180 defers elective
    // dental treatment; 140/90 is the classic hypertension band.
    private const int CriticalSystolic = 180;
    private const int WarningSystolic = 140;
    private const int WarningDiastolic = 90;

    /// <summary>Evaluates the recorded history into a single alert severity.</summary>
    public static MedicalAlertSeverity Evaluate(MedicalHistory? history)
    {
        if (history is null)
        {
            return MedicalAlertSeverity.None;
        }

        // A bleeding disorder changes the risk of every extraction.
        if (history.HasBleedingDisorder)
        {
            return MedicalAlertSeverity.Critical;
        }

        var (systolic, diastolic) = ParseBloodPressure(history.BloodPressureReading);
        if (systolic >= CriticalSystolic)
        {
            return MedicalAlertSeverity.Critical;
        }

        // A recorded reading we could not parse is still noteworthy.
        var hasUnparsedReading =
            systolic == 0 && !string.IsNullOrWhiteSpace(history.BloodPressureReading);

        var warning =
            history.HasDiabetes ||
            history.HasHypertension ||
            history.HasHeartDisease ||
            history.HasEpilepsy ||
            history.HasAsthma ||
            history.IsPregnant ||
            !string.IsNullOrWhiteSpace(history.Allergies) ||
            hasUnparsedReading ||
            (systolic >= WarningSystolic || diastolic >= WarningDiastolic);

        return warning ? MedicalAlertSeverity.Warning : MedicalAlertSeverity.None;
    }

    /// <summary>
    /// Parses a free-text reading such as "120/80" or "120 / 80". Returns
    /// (0, 0) when the text is empty or does not parse; the caller treats a
    /// non-empty but unparsed string as its own warning signal.
    /// </summary>
    private static (int Systolic, int Diastolic) ParseBloodPressure(string? reading)
    {
        if (string.IsNullOrWhiteSpace(reading))
        {
            return (0, 0);
        }

        var parts = reading.Split('/', 2);
        return (ParsePart(parts[0]), parts.Length > 1 ? ParsePart(parts[1]) : 0);
    }

    private static int ParsePart(string part)
    {
        return int.TryParse(part.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var value)
            && value >= 0 && value <= 300
            ? value
            : 0;
    }
}
