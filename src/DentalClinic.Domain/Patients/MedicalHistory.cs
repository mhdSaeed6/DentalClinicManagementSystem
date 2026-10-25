namespace DentalClinic.Domain.Patients;

public record MedicalHistory
{
    public bool HasDiabetes { get; init; }
    public bool HasHypertension { get; init; }
    public bool HasHeartDisease { get; init; }
    public bool HasBleedingDisorder { get; init; }
    public bool HasEpilepsy { get; init; }
    public bool HasAsthma { get; init; }
    public bool IsPregnant { get; init; }

    /// <summary>Weeks of pregnancy, only meaningful while <see cref="IsPregnant"/> is set.</summary>
    public int? PregnancyWeeks { get; init; }

    /// <summary>Free-text reading as noted by the clinician, e.g. "120/80".</summary>
    public string? BloodPressureReading { get; init; }
    public string? Allergies { get; init; }
    public string? Notes { get; init; }

    /// <summary>
    /// Severity computed by <see cref="MedicalAlertEvaluator"/> — one shared
    /// rule set so every screen agrees on what counts as an alert.
    /// </summary>
    public MedicalAlertSeverity AlertSeverity => MedicalAlertEvaluator.Evaluate(this);
}