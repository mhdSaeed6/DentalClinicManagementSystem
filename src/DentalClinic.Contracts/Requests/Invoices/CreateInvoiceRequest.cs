namespace DentalClinic.Contracts.Requests.Invoices;

public class CreateInvoiceRequest
{
    public Guid PatientId { get; set; }
    public Guid? AppointmentId { get; set; }
    public decimal TotalAmount { get; set; }
}