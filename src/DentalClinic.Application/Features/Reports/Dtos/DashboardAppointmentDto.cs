using DentalClinic.Domain.Appointments.Enums;

namespace DentalClinic.Application.Features.Reports.Dtos;

/// <summary>
/// One appointment as a dashboard/report row. The patient, doctor and service
/// names are resolved on the server so the page never has to hold three lookup
/// lists just to label a handful of rows.
/// </summary>
public sealed record DashboardAppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string? ServiceName,
    DateTime ScheduledDateTime,
    int DurationInMinutes,
    AppointmentStatus Status);