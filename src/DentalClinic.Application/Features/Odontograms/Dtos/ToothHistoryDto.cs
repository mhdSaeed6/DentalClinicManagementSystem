namespace DentalClinic.Application.Features.Odontograms.Dtos;

/// <summary>
/// A treatment record's entry in the patient odontogram. <c>ToothNumber</c> is
/// null when the treatment is not tied to a specific tooth; the UI's per-tooth
/// grouping simply skips it.
/// </summary>
public sealed record ToothHistoryDto(
    int? ToothNumber,
    Guid TreatmentRecordId,
    DateTimeOffset RecordedAtUtc,
    Guid DoctorId,
    string ProcedureDetails,
    decimal Cost);