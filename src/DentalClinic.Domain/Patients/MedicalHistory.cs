namespace DentalClinic.Domain.Patients;

public record MedicalHistory
{
    public bool HasDiabetes { get; init; }
    public bool HasHypertension { get; init; }
    public bool HasHeartDisease { get; init; }
    public string? Allergies { get; init; }
    public string? Notes { get; init; }
}