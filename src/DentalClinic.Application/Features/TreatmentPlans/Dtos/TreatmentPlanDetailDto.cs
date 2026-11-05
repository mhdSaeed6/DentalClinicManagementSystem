using DentalClinic.Domain.TreatmentPlans;

namespace DentalClinic.Application.Features.TreatmentPlans.Dtos;

/// <summary>
/// The plan header with its (non-deleted) items and aggregate totals —
/// backing the plan editor and the print sheet.
/// </summary>
public sealed record TreatmentPlanDetailDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    string PatientName,
    string DoctorName,
    string Title,
    string? Notes,
    TreatmentPlanStatus Status,
    DateTimeOffset CreatedOnUtc,
    int ItemCount,
    decimal EstimatedTotal,
    decimal PerformedTotal,
    IReadOnlyList<TreatmentPlanItemDto> Items);