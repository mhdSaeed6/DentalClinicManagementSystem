using DentalClinic.Domain.Appointments.Enums;

namespace DentalClinic.Application.Features.Appointments.Dtos;

public sealed record AppointmentDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    Guid ServiceId,
    DateTime ScheduledDateTime,
    int DurationInMinutes,
    AppointmentStatus Status,
    string? Notes);
