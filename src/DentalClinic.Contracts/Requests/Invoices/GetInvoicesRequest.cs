namespace DentalClinic.Contracts.Requests.Invoices;

public class GetInvoicesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
}

