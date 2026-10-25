namespace DentalClinic.Contracts.Requests.Invoices;

public class GetInvoiceSummaryRequest
{
    /// <summary>Optional patient filter, e.g. the balance of a single patient file.</summary>
    public Guid? PatientId { get; set; }
}
