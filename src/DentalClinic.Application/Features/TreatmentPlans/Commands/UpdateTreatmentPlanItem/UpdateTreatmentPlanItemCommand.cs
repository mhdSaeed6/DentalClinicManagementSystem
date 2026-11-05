using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlanItem;

public sealed record UpdateTreatmentPlanItemCommand(
    Guid PlanId,
    Guid ItemId,
    int? ToothNumber,
    string ProcedureDetails,
    decimal? EstimatedCost,
    int SessionNumber) : IRequest<Result<TreatmentPlanItemDto>>;