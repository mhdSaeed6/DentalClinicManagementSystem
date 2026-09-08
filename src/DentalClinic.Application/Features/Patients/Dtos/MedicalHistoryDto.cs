namespace DentalClinic.Application.Features.Patients.Dtos;

public sealed record MedicalHistoryDto(
    bool HasDiabetes,
    bool HasHypertension,
    bool HasHeartDisease,
    string? Allergies,
    string? Notes);