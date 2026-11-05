using DentalClinic.Application.Features.Clinics.Dtos;
using DentalClinic.Domain.Clinics;

namespace DentalClinic.Application.Features.Clinics.Mappers;

public static class ClinicSettingsMappingExtensions
{
    public static ClinicSettingsDto ToDto(this ClinicSetting entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ClinicSettingsDto(
            entity.Name,
            entity.NameArabic,
            entity.LogoUrl,
            entity.Phone,
            entity.Address,
            entity.Email,
            entity.TimeZoneId,
            entity.BaseCurrency,
            entity.SecondaryCurrency,
            entity.DefaultCountryCode);
    }
}