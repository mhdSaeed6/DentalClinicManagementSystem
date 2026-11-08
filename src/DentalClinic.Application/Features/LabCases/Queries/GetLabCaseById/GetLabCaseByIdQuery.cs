using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.LabCases.Queries.GetLabCaseById;

public sealed record GetLabCaseByIdQuery(Guid LabCaseId, DoctorAccessScope Scope)
    : IRequest<Result<LabCaseDto>>;