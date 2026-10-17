using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Invoices;

public class UpdateInvoiceLineRequest
{
    [Required]
    public Guid LineId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }
}

public class UpdateInvoiceDraftRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<UpdateInvoiceLineRequest> Lines { get; set; } = [];
}

public class RefreshInvoiceDraftRequest
{
    /// <summary>
    /// Must be true to discard manual adjustments that are already on the draft.
    /// </summary>
    public bool ConfirmDiscardManualAdjustments { get; set; }
}