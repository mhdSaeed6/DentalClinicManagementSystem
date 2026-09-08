using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Patients.Commands.RemovePatient;

public sealed record RemovePatientCommand(Guid PatientId) : IRequest<Result<Deleted>>;