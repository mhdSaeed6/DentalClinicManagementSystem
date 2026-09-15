namespace DentalClinic.Contracts.Requests.TreatmentRecords;

public class GetTreatmentRecordsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? AppointmentId { get; set; }
}

