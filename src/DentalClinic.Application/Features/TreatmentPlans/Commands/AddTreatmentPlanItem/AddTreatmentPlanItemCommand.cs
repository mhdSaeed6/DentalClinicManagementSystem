using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.AddTreatmentPlanItem;

// Patient and doctor identities are never client-supplied for plan items —
// they derive from the plan itself, so scoping can never drift.
public sealed record AddTreatmentPlanItemCommand(
    Guid PlanId,
    int? ToothNumber,
    string ProcedureDetails,
    decimal? EstimatedCost,
    int SessionNumber = 1) : IRequest<Result<TreatmentPlanItemDto>>;