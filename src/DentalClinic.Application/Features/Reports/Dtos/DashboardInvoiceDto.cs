using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Application.Features.Reports.Dtos;

/// <summary>
/// One invoice as a dashboard/report row, with the patient name resolved on the
/// server and the remaining balance computed in SQL.
/// </summary>
public sealed record DashboardInvoiceDto(
    Guid Id,
    string? InvoiceNumber,
    Guid PatientId,
    string PatientName,
    decimal TotalAmount,
    decimal RemainingAmount,
    PaymentStatus Status,
    InvoiceLifecycleStatus LifecycleStatus);