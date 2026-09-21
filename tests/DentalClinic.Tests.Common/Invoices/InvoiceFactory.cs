using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;

namespace DentalClinic.Tests.Common.Invoices;

public static class InvoiceFactory
{
    public static Result<Invoice> CreateInvoice(
        Guid patientId,
        decimal totalAmount = 100.0m,
        Guid? appointmentId = null)
    {
        return Invoice.Create(patientId, totalAmount, appointmentId);
    }
}