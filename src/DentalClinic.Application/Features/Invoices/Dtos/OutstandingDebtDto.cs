namespace DentalClinic.Application.Features.Invoices.Dtos;

/// <summary>
/// One row per patient with an outstanding balance on issued invoices.
/// Constructed in the handler, which resolves the patient names in a single
/// round trip (same pattern as the invoice list).
/// </summary>
public sealed record OutstandingDebtDto(
    Guid PatientId,
    string PatientName,
    int OpenInvoiceCount,
    decimal TotalOutstanding,
    DateTimeOffset? OldestOpenInvoiceDateUtc);
