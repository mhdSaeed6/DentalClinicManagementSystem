using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.TreatmentRecords;

public class CreateTreatmentRecordRequest
{
    [Required(ErrorMessage = "Patient ID is required.")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Doctor ID is required.")]
    public Guid DoctorId { get; set; }

    public Guid? AppointmentId { get; set; }

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