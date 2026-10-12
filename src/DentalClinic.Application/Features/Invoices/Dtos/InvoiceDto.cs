using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Application.Features.Invoices.Dtos;

public sealed record InvoiceDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid? AppointmentId,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus Status,
    IReadOnlyCollection<PaymentDto> Payments);
