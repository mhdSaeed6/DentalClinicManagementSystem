namespace DentalClinic.Application.Features.Invoices.Dtos;

public sealed record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    DateTime PaidAtUtc,
    string? TransactionNotes);
