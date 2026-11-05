using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.ActivateTreatmentPlan;

public sealed record ActivateTreatmentPlanCommand(Guid PlanId) : IRequest<Result<Updated>>;