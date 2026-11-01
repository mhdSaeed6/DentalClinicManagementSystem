using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.TreatmentPlans;

public sealed class CreateTreatmentPlanRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }
}