using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Doctors.Commands.RemoveDoctor;

public sealed record RemoveDoctorCommand(Guid DoctorId) : IRequest<Result<Deleted>>;
