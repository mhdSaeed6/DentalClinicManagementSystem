using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Appointments.Events;

/// <summary>
/// Raised after an appointment has been marked as a no-show. Carries only identifiers.
/// </summary>
public sealed record AppointmentMarkedAsNoShowEvent(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId) : DomainEvent;
