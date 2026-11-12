namespace DentalClinic.Domain.Common.Interfaces;

/// <summary>
/// Marker interface for aggregates whose create/update/delete changes must be
/// captured in the audit trail.
///
/// Only the main aggregates are marked (Patient, Appointment, Invoice,
/// TreatmentRecord). Auditing every table would multiply write volume and turn
/// the history into noise, so the marker is opt-in rather than opt-out.
/// </summary>
public interface IRequireAuditTrail
{
}
