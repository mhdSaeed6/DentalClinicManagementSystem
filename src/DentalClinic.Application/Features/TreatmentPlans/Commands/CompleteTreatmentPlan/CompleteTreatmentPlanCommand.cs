using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CompleteTreatmentPlan;

public sealed record CompleteTreatmentPlanCommand(Guid PlanId) : IRequest<Result<Updated>>;