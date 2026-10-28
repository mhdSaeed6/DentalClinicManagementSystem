using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.TreatmentRecords;

public class UpdateTreatmentRecordRequest
{
    [Required(ErrorMessage = "Patient ID is required.")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Doctor ID is required.")]
    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

    // S8: nullable — updating with null clears the tooth link; a provided value
    // must still land in the FDI range (real FDI validation is enforced by the
    // application validators, per the S0 single-source rule).
    [Range(11, 85, ErrorMessage = "Tooth number must be a valid FDI number (11-85).")]
    public int? ToothNumber { get; set; }

    [Required(ErrorMessage = "Procedure details are required.")]
    [StringLength(1000, ErrorMessage = "Procedure details cannot exceed 1000 characters.")]
    public string ProcedureDetails { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Cost cannot be negative.")]
    public decimal Cost { get; set; }
}