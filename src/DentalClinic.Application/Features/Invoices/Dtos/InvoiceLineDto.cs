namespace DentalClinic.Application.Features.Invoices.Dtos;

/// <summary>
/// A single, clearly identifiable charge that was billed on an invoice.
/// Lines are a transient breakdown of how the invoice total was computed;
/// the invoice itself persists the total as a price snapshot.
/// </summary>
public sealed record InvoiceLineDto(
    string Description,
    decimal Amount);
