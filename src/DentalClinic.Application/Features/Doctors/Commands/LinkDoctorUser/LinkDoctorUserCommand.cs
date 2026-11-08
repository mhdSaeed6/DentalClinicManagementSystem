using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Doctors.Commands.LinkDoctorUser;

public sealed record LinkDoctorUserCommand(
    Guid DoctorId,
    string UserId) : IRequest<Result<Updated>>;