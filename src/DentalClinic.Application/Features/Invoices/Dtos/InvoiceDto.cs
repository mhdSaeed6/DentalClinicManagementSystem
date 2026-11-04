using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Application.Features.Invoices.Dtos;

public sealed record InvoiceDto(
    Guid Id,
    string? InvoiceNumber,
    Guid PatientId,
    string PatientName,
    Guid? AppointmentId,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus Status,
    InvoiceLifecycleStatus LifecycleStatus,
    DateTimeOffset? IssuedAtUtc,
    IReadOnlyCollection<InvoiceLineDto> Lines,
    IReadOnlyCollection<PaymentDto> Payments);