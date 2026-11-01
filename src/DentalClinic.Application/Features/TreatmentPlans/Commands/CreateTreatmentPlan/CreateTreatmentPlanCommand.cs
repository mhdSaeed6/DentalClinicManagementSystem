using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

public sealed record CreateTreatmentPlanCommand(
    Guid PatientId,
    Guid DoctorId,
    string Title,
    string? Notes) : IRequest<Result<TreatmentPlanDto>>;