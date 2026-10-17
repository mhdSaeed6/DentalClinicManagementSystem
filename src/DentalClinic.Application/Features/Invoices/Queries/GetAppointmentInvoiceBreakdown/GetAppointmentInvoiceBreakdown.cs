using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Invoices.Queries.GetAppointmentInvoiceBreakdown;

public record GetAppointmentInvoiceBreakdownQuery(Guid AppointmentId)
    : IRequest<Result<AppointmentInvoiceBreakdownDto>>;

/// <summary>
/// The invoice-relevant charges for an appointment, computed with the exact same
/// rules the draft generator uses. Lets the drawer and the manual-invoice form
/// show a trustworthy estimate before anything is persisted.
/// </summary>
public sealed record AppointmentInvoiceBreakdownDto(
    Guid AppointmentId,
    string? ServiceName,
    decimal ServiceSubtotal,
    decimal TreatmentSubtotal,
    decimal EstimatedTotal,
    int TreatmentCount,
    bool HasBillableItems);

public class GetAppointmentInvoiceBreakdownQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAppointmentInvoiceBreakdownQuery, Result<AppointmentInvoiceBreakdownDto>>
{
    public async Task<Result<AppointmentInvoiceBreakdownDto>> Handle(
        GetAppointmentInvoiceBreakdownQuery request,
        CancellationToken cancellationToken)
    {
        var appointment = await context.Appointments
            .Include(a => a.Service)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            return ApplicationErrors.AppointmentNotFound;
        }

        var treatments = await context.TreatmentRecords
            .AsNoTracking()
            .Where(t => t.AppointmentId == appointment.Id)
            .ToListAsync(cancellationToken);

        // Reuse the generator's pricing rules instead of duplicating them.
        var built = GenerateAppointmentInvoiceCommandHandler.BuildLines(appointment, treatments);
        if (built.IsError)
        {
            return built.Errors;
        }

        var (lines, serviceSubtotal, treatmentSubtotal) = built.Value;
        var treatmentCount = lines.Count - (serviceSubtotal > 0 ? 1 : 0);

        return new AppointmentInvoiceBreakdownDto(
            appointment.Id,
            appointment.Service?.Name,
            serviceSubtotal,
            treatmentSubtotal,
            serviceSubtotal + treatmentSubtotal,
            treatmentCount,
            lines.Count > 0);
    }
}