namespace DentalClinic.Application.Features.Patients.Dtos;

public sealed record PatientDto(
    Guid Id,
    string FirstName,
    string LastName,
    string PrimaryPhone,
    string? SecondaryPhone,
    bool HasWhatsAppOnPrimary,
    DateTime DateOfBirth,
    int Gender,
    MedicalHistoryDto MedicalHistory);
