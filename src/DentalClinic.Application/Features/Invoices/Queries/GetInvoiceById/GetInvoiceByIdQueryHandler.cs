using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler(
    ILogger<GetInvoiceByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await context.Invoices.Include(i => i.Payments).AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogWarning("Invoice retrieval failed. Invoice {InvoiceId} not found.", request.InvoiceId);
            return ApplicationErrors.InvoiceNotFound;
        }

        // Trusted, server-derived scope: a doctor may only read invoices tied to
        // one of their own appointments.
        if (request.Scope.IsRestricted)
        {
            var scopeDoctorId = request.Scope.DoctorId;
            var canAccess = invoice.AppointmentId is { } appointmentId &&
                await context.Appointments.AnyAsync(
                    a => a.Id == appointmentId && a.DoctorId == scopeDoctorId,
                    cancellationToken);

            if (!canAccess)
            {
                logger.LogWarning("Invoice {InvoiceId} access denied for doctor {DoctorId}.", request.InvoiceId, scopeDoctorId);
                return ApplicationErrors.InvoiceAccessForbidden;
            }
        }

        var patientName = await context.Patients
            .Where(p => p.Id == invoice.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        return invoice.ToDto(patientName);
    }
}
