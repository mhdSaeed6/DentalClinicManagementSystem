using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Appointments.Commands.RescheduleAppointment;

public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateTime ScheduledDateTime,
    int DurationInMinutes) : IRequest<Result<AppointmentDto>>;
