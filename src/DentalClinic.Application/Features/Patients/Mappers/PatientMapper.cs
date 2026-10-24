using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Application.Features.Patients.Mappers;

public static class PatientMapper
{
    public static PatientDto ToDto(this Patient entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PatientDto(
            entity.Id,
            entity.FirstName,
            entity.LastName,
            entity.ContactInfo.PrimaryPhone,
            entity.ContactInfo.SecondaryPhone,
            entity.ContactInfo.HasWhatsAppOnPrimary,
            entity.DateOfBirth,
            (int)entity.Gender,
            entity.MedicalHistory.ToDto() );
    }

    public static List<PatientDto> ToDtos(this IEnumerable<Patient> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }

    public static MedicalHistoryDto ToDto(this MedicalHistory entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new MedicalHistoryDto(
            entity.HasDiabetes,
            entity.HasHypertension,
            entity.HasHeartDisease,
            entity.HasBleedingDisorder,
            entity.HasEpilepsy,
            entity.HasAsthma,
            entity.IsPregnant,
            entity.PregnancyWeeks,
            entity.BloodPressureReading,
            entity.Allergies,
            entity.Notes,
            (int)entity.AlertSeverity);
    }
}