using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Appointments;

/// <summary>
/// Optional body for completing an appointment. When <see cref="Treatments"/> is
/// empty or omitted, the appointment is completed as a consultation-only visit.
/// When <see cref="ServiceId"/> is supplied it replaces the appointment's booked
/// service (it must reference an existing, active service); when omitted, the
/// current service is left unchanged. The service definition itself is never
/// modified. The patient, doctor and appointment link are derived from the route
/// appointment and are intentionally not accepted from the client.
/// </summary>
public class CompleteAppointmentRequest
{
    public List<CompleteAppointmentTreatmentRequest>? Treatments { get; set; }

    /// <summary>
    /// The service actually carried out. Leave <c>null</c> to keep the appointment's
    /// current service.
    /// </summary>
    public Guid? ServiceId { get; set; }
}

public class CompleteAppointmentTreatmentRequest
{
    // S8: nullable — a treatment may be recorded without a specific tooth; a
    // provided value must still land in the FDI range (real FDI validation is
    // enforced by the application validators, per the S0 single-source rule).
    [Range(11, 85, ErrorMessage = "Tooth number must be a valid FDI number (11-85).")]
    public int? ToothNumber { get; set; }

    [Required(ErrorMessage = "Procedure details are required.")]
    [StringLength(1000, ErrorMessage = "Procedure details cannot exceed 1000 characters.")]
    public string ProcedureDetails { get; set; } = string.Empty;

    [Range(0, 10000000, ErrorMessage = "Cost cannot be negative.")]
    public decimal Cost { get; set; }
}
