using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;

public sealed record CreateAppointmentCommand(
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    DateTime ScheduledDateTime,
    int DurationInMinutes,
    string? Notes) : IRequest<Result<AppointmentDto>>;
