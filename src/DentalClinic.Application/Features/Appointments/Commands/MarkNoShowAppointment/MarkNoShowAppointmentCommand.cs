using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Appointments.Commands.MarkNoShowAppointment;

public sealed record MarkNoShowAppointmentCommand(Guid AppointmentId) : IRequest<Result<Updated>>;
