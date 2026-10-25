namespace DentalClinic.Application.Features.Odontograms.Dtos;

/// <summary>
/// One-round-trip odontogram payload for a patient: the live per-tooth condition
/// rows plus every treatment record (flat; the UI groups history by ToothNumber,
/// so a tooth with history but no condition row still shows its history marker).
/// </summary>
public sealed record PatientOdontogramDto(
    Guid PatientId,
    List<PatientToothSummaryDto> Teeth,
    List<ToothHistoryDto> History);