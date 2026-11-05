using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlan;

public sealed record CancelTreatmentPlanCommand(Guid PlanId) : IRequest<Result<Updated>>;