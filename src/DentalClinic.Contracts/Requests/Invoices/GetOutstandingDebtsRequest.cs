using System.ComponentModel.DataAnnotations;

namespace DentalClinic.Contracts.Requests.Invoices;

public class GetOutstandingDebtsRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than zero.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "Page size must not exceed 100.")]
    public int PageSize { get; set; } = 10;

    /// <summary>Only return debtors whose outstanding balance is at least this amount.</summary>
    [Range(0.01, 999999999, ErrorMessage = "Minimum amount must be positive.")]
    public decimal? MinAmount { get; set; }
}
