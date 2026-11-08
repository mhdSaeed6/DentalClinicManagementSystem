using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.PatientImages.Queries.GetPatientImageContent;

public sealed record GetPatientImageContentQuery(Guid PatientId, Guid ImageId, PatientAccessScope Scope)
    : IRequest<Result<PatientImageContent>>;