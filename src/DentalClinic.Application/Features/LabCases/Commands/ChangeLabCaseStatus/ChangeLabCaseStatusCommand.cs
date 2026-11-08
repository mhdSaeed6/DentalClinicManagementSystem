using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Labs;

using MediatR;

namespace DentalClinic.Application.Features.LabCases.Commands.ChangeLabCaseStatus;

public sealed record ChangeLabCaseStatusCommand(
    Guid LabCaseId,
    LabCaseStatus NewStatus,
    DateTimeOffset OccurredAtUtc) : IRequest<Result<LabCaseDto>>;