namespace DentalClinic.Application.Features.Reports.Dtos;

/// <summary>
/// What one doctor did inside the report window: appointments booked with them
/// and the invoices issued against those appointments.
/// </summary>
/// <remarks>
/// An invoice belongs to a doctor only through its appointment. A standalone
/// invoice has no owning doctor, so it stays out of every doctor's row rather
/// than being attributed to whoever happens to be looking — the same
/// fail-closed rule the invoice scope applies. Commission is deliberately not
/// computed here; the business rule has not been written down yet.
/// </remarks>
public sealed record DoctorProductivityDto(
    Guid DoctorId,
    string DoctorName,
    int AppointmentCount,
    int IssuedInvoiceCount,
    decimal BilledAmount,
    decimal PaidAmount);