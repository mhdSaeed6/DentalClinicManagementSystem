namespace DentalClinic.Application.Features.TreatmentRecords.Dtos;

/// <summary>
/// A treatment record as returned by the API. <c>ToothNumber</c> is null when
/// the treatment is not tied to a specific tooth (S8).
/// </summary>
public sealed record TreatmentRecordDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    Guid? AppointmentId,
    int? ToothNumber,
    string ProcedureDetails,
    decimal Cost);
