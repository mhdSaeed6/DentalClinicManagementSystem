namespace DentalClinic.Application.Features.Invoices.Dtos;

/// <summary>
/// A single persisted charge on an invoice. Each line is a price snapshot taken
/// when the line was created, so later catalog or treatment edits never silently
/// change an already generated invoice.
/// </summary>
public sealed record InvoiceLineDto(
    Guid Id,
    string Description,
    decimal Amount);