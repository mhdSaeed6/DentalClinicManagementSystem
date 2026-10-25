namespace DentalClinic.Application.Features.Patients.Dtos;

/// <summary>
/// Patient medical history as rendered by any client. The final
/// <see cref="AlertSeverity"/> parameter is a
/// <see cref="Domain.Patients.MedicalAlertSeverity"/> value computed on the
/// server, so every screen shows the same alert level without re-deriving it.
/// </summary>
public sealed record MedicalHistoryDto(
    bool HasDiabetes,
    bool HasHypertension,
    bool HasHeartDisease,
    bool HasBleedingDisorder,
    bool HasEpilepsy,
    bool HasAsthma,
    bool IsPregnant,
    int? PregnancyWeeks,
    string? BloodPressureReading,
    string? Allergies,
    string? Notes,
    int AlertSeverity);
