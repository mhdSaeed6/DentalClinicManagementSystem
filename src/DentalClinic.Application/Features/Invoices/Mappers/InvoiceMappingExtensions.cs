using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Invoices;

namespace DentalClinic.Application.Features.Invoices.Mappers;

public static class InvoiceMappingExtensions
{
    public static InvoiceDto ToDto(this Invoice entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new InvoiceDto(
            entity.Id,
            entity.PatientId,
            entity.AppointmentId,
            entity.TotalAmount,
            entity.PaidAmount,
            entity.RemainingAmount,
            entity.Status,
            [.. entity.Payments.Select(payment => payment.ToDto())]);
    }

    public static PaymentDto ToDto(this Payment entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PaymentDto(
            entity.Id,
            entity.InvoiceId,
            entity.Amount,
            entity.PaidAtUtc,
            entity.TransactionNotes);
    }

    public static List<InvoiceDto> ToDtos(this IEnumerable<Invoice> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
