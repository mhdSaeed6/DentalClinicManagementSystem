using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Doctors.Commands.UnlinkDoctorUser;

public sealed record UnlinkDoctorUserCommand(Guid DoctorId) : IRequest<Result<Updated>>;