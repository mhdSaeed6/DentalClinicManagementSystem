using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.ReopenTreatmentPlanItem;

// D-P4: a performed item may return to Planned — unless it is already part of
// an issued invoice, which the handler blocks deterministically.
public sealed record ReopenTreatmentPlanItemCommand(
    Guid PlanId,
    Guid ItemId) : IRequest<Result<TreatmentPlanItemDto>>;