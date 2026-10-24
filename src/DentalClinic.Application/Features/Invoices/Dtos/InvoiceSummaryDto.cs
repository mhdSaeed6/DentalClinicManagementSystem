namespace DentalClinic.Application.Features.Invoices.Dtos;

/// <summary>
/// Money totals over the invoices visible to the caller — always computed on
/// the server over the <em>full</em> matching set, never over a single page
/// (fixes the wrong client-side sums, AUDIT.md D-09 / §8.8). Drafts are
/// estimates rather than real debt, so only issued invoices take part in the
/// money totals.
/// </summary>
public sealed record InvoiceSummaryDto(
    int TotalInvoiceCount,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalOutstanding,
    int OpenInvoiceCount,
    DateTimeOffset? OldestUnpaidInvoiceDateUtc);
