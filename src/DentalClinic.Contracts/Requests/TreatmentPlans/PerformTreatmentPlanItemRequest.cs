using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.TreatmentPlans;

public sealed class PerformTreatmentPlanItemRequest
{
    [Range(0, 10000000)]
    public decimal ActualCost { get; set; }

    public Guid? AppointmentId { get; set; }
}