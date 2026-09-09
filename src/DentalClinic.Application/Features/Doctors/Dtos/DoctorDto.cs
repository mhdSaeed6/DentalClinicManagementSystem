using DentalClinic.Domain.Common.Enums;

namespace DentalClinic.Application.Features.Doctors.Dtos;

public sealed record DoctorDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Specialization,
    string PrimaryPhone,
    string? SecondaryPhone,
    bool HasWhatsAppOnPrimary,
    string? SocialMediaLink,
    Gender Gender,
    bool IsActive);
