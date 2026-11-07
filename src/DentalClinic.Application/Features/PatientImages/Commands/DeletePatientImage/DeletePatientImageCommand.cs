using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.PatientImages.Commands.DeletePatientImage;

public sealed record DeletePatientImageCommand(Guid PatientId, Guid ImageId) : IRequest<Result<Deleted>>;