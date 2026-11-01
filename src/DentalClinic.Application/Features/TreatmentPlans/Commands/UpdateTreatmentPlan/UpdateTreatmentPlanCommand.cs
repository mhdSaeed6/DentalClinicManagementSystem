using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlan;

public sealed record UpdateTreatmentPlanCommand(
    Guid PlanId,
    string Title,
    string? Notes) : IRequest<Result<Updated>>;