using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Appointments.Events;

/// <summary>
/// Raised after an appointment has been rescheduled to a new date/time.
/// Carries only identifiers and the previous/new scheduled times.
/// </summary>
public sealed record AppointmentRescheduledEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime PreviousScheduledDateTime,
    DateTime ScheduledDateTime) : DomainEvent;
