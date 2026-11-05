using DentalClinic.Domain.TreatmentRecords;

namespace DentalClinic.Application.Features.TreatmentPlans.Dtos;

/// <summary>
/// A plan item as returned by the API — a treatment record in its plan role.
/// <c>EstimatedCost</c> is the plan-time estimate (never billed); <c>Cost</c>
/// stays the actual charged amount. A directly created (ad-hoc) record has a
/// null <c>PlanId</c> and is never returned as a plan item.
/// </summary>
public sealed record TreatmentPlanItemDto(
    Guid Id,
    Guid? PlanId,
    Guid PatientId,
    Guid DoctorId,
    Guid? AppointmentId,
    int? ToothNumber,
    string ProcedureDetails,
    TreatmentRecordStatus Status,
    decimal? EstimatedCost,
    decimal Cost,
    int SessionNumber);