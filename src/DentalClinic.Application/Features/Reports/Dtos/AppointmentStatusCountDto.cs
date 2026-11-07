using DentalClinic.Domain.Appointments.Enums;

namespace DentalClinic.Application.Features.Reports.Dtos;

/// <summary>
/// How many appointments sat in one status inside the report window. Every
/// status is returned, including the zeroes, so the UI never has to invent a
/// missing bar.
/// </summary>
public sealed record AppointmentStatusCountDto(AppointmentStatus Status, int Count);