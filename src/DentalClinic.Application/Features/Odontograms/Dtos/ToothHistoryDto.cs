namespace DentalClinic.Application.Features.Odontograms.Dtos;

public sealed record ToothHistoryDto(
    int ToothNumber,
    Guid TreatmentRecordId,
    DateTimeOffset RecordedAtUtc,
    Guid DoctorId,
    string ProcedureDetails,
    decimal Cost);