namespace DentalClinic.Contracts.Requests.TreatmentPlans;

public sealed class GetTreatmentPlansRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public int? Status { get; set; }
}