namespace DentalClinic.Application.Features.TreatmentRecords.Dtos;

public sealed record TreatmentRecordDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    Guid? AppointmentId,
    int ToothNumber,
    string ProcedureDetails,
    decimal Cost);
