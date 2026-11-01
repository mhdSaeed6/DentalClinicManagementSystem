using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.TreatmentPlans;

public sealed class UpdateTreatmentPlanRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }
}