using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Application.Features.PatientImages.Mappers;

public static class PatientImageMappingExtensions
{
    public static PatientImageDto ToDto(this PatientImage entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PatientImageDto(
            entity.Id,
            entity.PatientId,
            entity.TreatmentRecordId,
            entity.ToothNumber,
            entity.Kind,
            entity.FileName,
            entity.ContentType,
            entity.SizeBytes,
            entity.CapturedAtUtc,
            entity.UploadedBy,
            entity.CreatedAtUtc);
    }

    public static List<PatientImageDto> ToDtos(this IEnumerable<PatientImage> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}