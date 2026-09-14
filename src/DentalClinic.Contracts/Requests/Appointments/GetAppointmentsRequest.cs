namespace DentalClinic.Contracts.Requests.Appointments;

public class GetAppointmentsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? ServiceId { get; set; }

}