using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Invoices;

namespace DentalClinic.Application.Features.Invoices.Mappers;

public static class InvoiceMappingExtensions
{
    public static InvoiceDto ToDto(this Invoice entity, string? patientName = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new InvoiceDto(
            entity.Id,
            entity.PatientId,
            patientName ?? string.Empty,
            entity.AppointmentId,
            entity.TotalAmount,
            entity.PaidAmount,
            entity.RemainingAmount,
            entity.Status,
            entity.LifecycleStatus,
            [.. entity.Lines.Select(line => line.ToDto())],
            [.. entity.Payments.Select(payment => payment.ToDto())]);
    }

    public static InvoiceLineDto ToDto(this InvoiceLine entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new InvoiceLineDto(entity.Id, entity.Description, entity.Amount);
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

    public static List<InvoiceDto> ToDtos(this IEnumerable<Invoice> entities, IReadOnlyDictionary<Guid, string>? patientNames = null)
    {
        return [.. entities.Select(e =>
            e.ToDto(patientNames is not null && patientNames.TryGetValue(e.PatientId, out var name) ? name : null))];
    }
}