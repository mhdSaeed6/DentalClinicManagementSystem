using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Doctors;

namespace DentalClinic.Application.Features.Doctors.Mappers;

public static class DoctorMappingExtensions
{
    public static DoctorDto ToDto(this Doctor entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new DoctorDto(
            entity.Id,
            entity.FirstName,
            entity.LastName,
            entity.Specialization,
            entity.ContactInfo.PrimaryPhone,
            entity.ContactInfo.SecondaryPhone,
            entity.ContactInfo.HasWhatsAppOnPrimary,
            entity.ContactInfo.SocialMediaLink,
            entity.Gender,
            entity.IsActive);
    }

    public static List<DoctorDto> ToDtos(this IEnumerable<Doctor> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
