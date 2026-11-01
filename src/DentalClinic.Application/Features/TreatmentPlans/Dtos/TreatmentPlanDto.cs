using DentalClinic.Domain.TreatmentPlans;

namespace DentalClinic.Application.Features.TreatmentPlans.Dtos;

/// <summary>
/// A treatment plan header with its aggregate totals. EstimatedTotal covers
/// planned items (never billed); PerformedTotal covers charged performed work.
/// Cancelled items count against neither (they remain in history only).
/// </summary>
public sealed record TreatmentPlanDto(
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
    decimal PerformedTotal);