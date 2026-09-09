using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Invoices.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler(
    ILogger<CreateInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateInvoiceCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating invoice for patient {PatientId}", request.PatientId);

        var patientExists = await context.Patients.AnyAsync(p => p.Id == request.PatientId, cancellationToken);
        if (!patientExists)
        {
            logger.LogWarning("Invoice creation failed. Patient {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        if (request.AppointmentId.HasValue)
        {
            var appointmentExists = await context.Appointments.AnyAsync(a => a.Id == request.AppointmentId.Value, cancellationToken);
            if (!appointmentExists)
            {
                logger.LogWarning("Invoice creation failed. Appointment {AppointmentId} not found.", request.AppointmentId);
                return ApplicationErrors.AppointmentNotFound;
            }
        }

        var invoiceResult = Invoice.Create(request.PatientId, request.TotalAmount, request.AppointmentId);
        if (invoiceResult.IsError)
        {
            logger.LogWarning("Failed to create invoice for patient {PatientId}. Errors: {@Errors}", request.PatientId, invoiceResult.Errors);
            return invoiceResult.Errors;
        }

        var invoice = invoiceResult.Value;

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("invoice", cancellationToken);

        logger.LogInformation("Invoice created successfully with ID: {InvoiceId}", invoice.Id);

        return invoice.ToDto();
    }
}
