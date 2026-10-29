using DentalClinic.Application.Features.Odontograms.Dtos;
using DentalClinic.Domain.Odontograms;
using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Application.Features.Odontograms.Mappers;

public static class OdontogramMappingExtensions
{
    public static PatientToothSummaryDto ToSummaryDto(this PatientTooth entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PatientToothSummaryDto(
            entity.ToothNumber,
            entity.Condition,
            entity.Surface,
            entity.Notes,
            entity.LastModifiedUtc);
    }

    public static List<PatientToothSummaryDto> ToSummaryDtos(this IEnumerable<PatientTooth> entities)
    {
        return [.. entities.Select(e => e.ToSummaryDto())];
    }

    // RecordedAtUtc maps from the audit CreatedAtUtc — history ordering stays
    // independent of whether the row was ever updated.
    public static ToothHistoryDto ToHistoryDto(this TreatmentRecord entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new ToothHistoryDto(
            entity.ToothNumber,
            entity.Id,
            entity.CreatedAtUtc,
            entity.DoctorId,
            entity.ProcedureDetails,
            entity.Cost);
    }

    public static List<ToothHistoryDto> ToHistoryDtos(this IEnumerable<TreatmentRecord> entities)
    {
        return [.. entities.Select(e => e.ToHistoryDto())];
    }
}