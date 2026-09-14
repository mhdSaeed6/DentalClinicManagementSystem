using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Invoices;

public class AddInvoicePaymentRequest
{
    [Range(0.01, 10000000, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }
}