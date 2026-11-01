using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.TreatmentPlans;

public sealed class AddTreatmentPlanItemRequest
{
    [Range(11, 85)]
    public int? ToothNumber { get; set; }

    [Required]
    [StringLength(1000)]
    public string ProcedureDetails { get; set; } = string.Empty;

    [Range(0, 10000000)]
    public decimal? EstimatedCost { get; set; }

    [Range(1, int.MaxValue)]
    public int SessionNumber { get; set; } = 1;
}