using DentalClinic.Domain.Invoices.Enums;

namespace DentalClinic.Application.Features.Invoices.Dtos;

/// <summary>
/// Result of generating (or reusing) the invoice for an appointment. Carries
/// everything the UI needs to review the outcome without a second round-trip.
/// </summary>
public sealed record GenerateAppointmentInvoiceResult(
    Guid InvoiceId,
    Guid PatientId,
    string PatientName,
    Guid AppointmentId,
    decimal ServiceSubtotal,
    decimal TreatmentSubtotal,
    decimal TotalAmount,
    PaymentStatus Status,
    InvoiceLifecycleStatus LifecycleStatus,
    bool ReusedExisting,
    IReadOnlyCollection<InvoiceLineDto> Lines);