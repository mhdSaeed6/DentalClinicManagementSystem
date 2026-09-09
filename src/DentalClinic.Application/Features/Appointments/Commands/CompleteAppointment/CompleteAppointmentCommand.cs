using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;

public sealed record CompleteAppointmentCommand(Guid AppointmentId) : IRequest<Result<Updated>>;
