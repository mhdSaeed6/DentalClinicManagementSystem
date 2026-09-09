using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Appointments.Commands.CancelAppointment;

public sealed record CancelAppointmentCommand(Guid AppointmentId) : IRequest<Result<Updated>>;
