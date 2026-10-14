using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Appointments.Events;

/// <summary>
/// Raised after a new appointment has been successfully persisted. Carries only
/// identifiers and the scheduled time; no patient or clinical details are included.
/// </summary>
public sealed record AppointmentCreatedEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid? ServiceId,
    DateTime ScheduledDateTime) : DomainEvent;
