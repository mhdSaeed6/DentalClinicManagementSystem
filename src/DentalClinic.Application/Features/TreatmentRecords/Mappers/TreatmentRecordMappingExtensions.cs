using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Application.Features.TreatmentRecords.Mappers;

public static class TreatmentRecordMappingExtensions
{
    public static TreatmentRecordDto ToDto(this TreatmentRecord entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TreatmentRecordDto(
            entity.Id,
            entity.PatientId,
            entity.DoctorId,
            entity.AppointmentId,
            entity.ToothNumber,
            entity.ProcedureDetails,
            entity.Cost);
    }

    public static List<TreatmentRecordDto> ToDtos(this IEnumerable<TreatmentRecord> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
