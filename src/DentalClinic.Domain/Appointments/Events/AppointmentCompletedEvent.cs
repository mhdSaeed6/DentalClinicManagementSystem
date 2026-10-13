using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Appointments.Events;

/// <summary>
/// Raised after an appointment has been completed. Carries only identifiers and
/// the scheduled time.
/// </summary>
public sealed record AppointmentCompletedEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime ScheduledDateTime) : DomainEvent;
