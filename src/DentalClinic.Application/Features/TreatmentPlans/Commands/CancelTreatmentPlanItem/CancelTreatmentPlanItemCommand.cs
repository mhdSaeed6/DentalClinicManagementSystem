using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlanItem;

// Cancelling stays visible in history: cancelled items are never billable and
// never disappear from the plan (they only stop rendering into totals).
public sealed record CancelTreatmentPlanItemCommand(
    Guid PlanId,
    Guid ItemId) : IRequest<Result<TreatmentPlanItemDto>>;