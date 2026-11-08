using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.LabCases.Commands.DeleteLabCase;

public sealed record DeleteLabCaseCommand(Guid LabCaseId) : IRequest<Result<Deleted>>;