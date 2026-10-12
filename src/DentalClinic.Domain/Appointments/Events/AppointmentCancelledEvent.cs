using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Appointments.Events;

/// <summary>
/// Raised after an appointment has been cancelled. Carries only identifiers.
/// </summary>
public sealed record AppointmentCancelledEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId) : DomainEvent;
