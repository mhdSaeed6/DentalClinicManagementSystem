using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Invoices;

public class AddInvoicePaymentRequest
{
    [Range(0.01, 10000000, ErrorMessage = "Payment amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
    public string? Notes { get; set; }

    /// <summary>
    /// How the payment was made, as an integer (<c>Cash=1, Card=2,
    /// BankTransfer=3, Other=4</c>). Omitted means cash, so an older client that
    /// knows nothing about this field keeps behaving exactly as before. The
    /// command validator owns enum validation.
    /// </summary>
    public int Method { get; set; } = 1;

    /// <summary>
    /// Optional card/transfer reference (at most 64 characters). Metadata only —
    /// it never affects the invoice balance.
    /// </summary>
    [StringLength(64, ErrorMessage = "Reference cannot exceed 64 characters.")]
    public string? Reference { get; set; }
}