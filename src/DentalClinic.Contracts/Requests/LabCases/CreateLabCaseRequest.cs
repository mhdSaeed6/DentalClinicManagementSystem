using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.LabCases;

public sealed class CreateLabCaseRequest
{
    [Required(ErrorMessage = "Patient ID is required.")]
    public Guid PatientId { get; set; }

    [Required(ErrorMessage = "Doctor ID is required.")]
    public Guid DoctorId { get; set; }

    public Guid? TreatmentRecordId { get; set; }

    // S8: nullable - a lab case may not be tied to a specific tooth; a provided
    // value must still land in the FDI range (real FDI validation is enforced by
    // the application validators).
    [Range(11, 85, ErrorMessage = "Tooth number must be a valid FDI number (11-85).")]
    public int? ToothNumber { get; set; }

    [Required(ErrorMessage = "The dental lab name is required.")]
    [StringLength(128, ErrorMessage = "The dental lab name cannot exceed 128 characters.")]
    public string LabName { get; set; } = string.Empty;

    [Required(ErrorMessage = "The case type is required.")]
    [StringLength(64, ErrorMessage = "The case type cannot exceed 64 characters.")]
    public string CaseType { get; set; } = string.Empty;

    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpectedReturnAtUtc { get; set; } = DateTimeOffset.UtcNow.AddDays(7);

    [Range(0, 100000, ErrorMessage = "Lab cost cannot be negative.")]
    public decimal Cost { get; set; }

    [StringLength(512, ErrorMessage = "Notes cannot exceed 512 characters.")]
    public string? Notes { get; set; }
}