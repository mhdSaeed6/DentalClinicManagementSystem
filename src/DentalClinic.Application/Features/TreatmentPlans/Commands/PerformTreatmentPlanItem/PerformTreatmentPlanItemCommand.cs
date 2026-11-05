using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.PerformTreatmentPlanItem;

// Sets the actual charged cost (D-P2) and optionally links the appointment the
// work was performed in; the estimate is kept untouched for the print sheet.
public sealed record PerformTreatmentPlanItemCommand(
    Guid PlanId,
    Guid ItemId,
    decimal ActualCost,
    Guid? AppointmentId = null) : IRequest<Result<TreatmentPlanItemDto>>;