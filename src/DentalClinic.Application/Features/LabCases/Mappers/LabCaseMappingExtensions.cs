using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Domain.Labs;

namespace DentalClinic.Application.Features.LabCases.Mappers;

public static class LabCaseMappingExtensions
{
    public static LabCaseDto ToDto(
        this LabCase entity,
        DateTimeOffset asOfUtc,
        string? patientName = null,
        string? doctorName = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new LabCaseDto(
            entity.Id,
            entity.PatientId,
            patientName,
            entity.DoctorId,
            doctorName,
            entity.TreatmentRecordId,
            entity.ToothNumber,
            entity.LabName,
            entity.CaseType,
            entity.Status,
            entity.SentAtUtc,
            entity.ExpectedReturnAtUtc,
            entity.ReturnedAtUtc,
            entity.Cost,
            entity.Notes,
            entity.IsOverdue(asOfUtc));
    }

    public static List<LabCaseDto> ToDtos(
        this IEnumerable<LabCase> entities,
        DateTimeOffset asOfUtc,
        Func<LabCase, string?>? patientName = null,
        Func<LabCase, string?>? doctorName = null)
    {
        return [.. entities.Select(e => e.ToDto(
            asOfUtc,
            patientName?.Invoke(e),
            doctorName?.Invoke(e)))];
    }
}