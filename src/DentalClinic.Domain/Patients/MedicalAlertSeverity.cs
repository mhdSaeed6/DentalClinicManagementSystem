namespace DentalClinic.Domain.Patients;

/// <summary>
/// Coarse alert level derived from a patient's medical history. The value is
/// computed once by <see cref="MedicalAlertEvaluator"/> and projected onto the
/// patient DTO, so every screen (registry, appointment drawer, dashboard) can
/// badge it without re-deriving its own rule set.
/// </summary>
public enum MedicalAlertSeverity
{
    /// <summary>No recorded condition requires attention.</summary>
    None = 0,

    /// <summary>A recorded condition warrants caution during treatment.</summary>
    Warning = 1,

    /// <summary>A recorded condition can have make treatment unsafe; confirm before treating.</summary>
    Critical = 2,
}
