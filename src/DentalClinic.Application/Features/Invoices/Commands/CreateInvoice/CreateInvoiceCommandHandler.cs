using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;
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
    HybridCache cache,
    IUser user)
    : IRequestHandler<CreateInvoiceCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating invoice for patient {PatientId}", request.PatientId);

        var patient = await context.Patients
            .Where(p => p.Id == request.PatientId)
            .Select(p => new { p.FirstName, p.LastName })
            .FirstOrDefaultAsync(cancellationToken);
        if (patient is null)
        {
            logger.LogWarning("Invoice creation failed. Patient {PatientId} not found.", request.PatientId);
            return ApplicationErrors.PatientNotFound;
        }

        // 1. An appointment-linked manual invoice must belong to the patient being
        //    billed. This is validated server-side; the form's selector is only
        //    a convenience and is never trusted.
        if (request.AppointmentId is { } appointmentId)
        {
            var appointment = await context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == appointmentId)
                .Select(a => new { a.PatientId, a.DoctorId })
                .FirstOrDefaultAsync(cancellationToken);

            if (appointment is null)
            {
                logger.LogWarning("Invoice creation failed. Appointment {AppointmentId} not found.", appointmentId);
                return ApplicationErrors.AppointmentNotFound;
            }

            // Ownership: an appointment-linked invoice belongs to the
            // appointment's doctor, so a doctor may only bill their own visit.
            if (!user.CanAccessAppointment(appointment.DoctorId))
            {
                logger.LogWarning(
                    "Invoice creation denied for user {UserId}. Appointment {AppointmentId} belongs to doctor {DoctorId}.",
                    user.Id,
                    appointmentId,
                    appointment.DoctorId);
                return ApplicationErrors.AppointmentAccessForbidden;
            }

            if (appointment.PatientId != request.PatientId)
            {
                logger.LogWarning(
                    "Invoice creation refused. Appointment {AppointmentId} belongs to patient {OwnerId}, not {PatientId}.",
                    appointmentId,
                    appointment.PatientId,
                    request.PatientId);
                return InvoiceErrors.AppointmentPatientMismatch;
            }

            // Guard against billing the same visit twice through the manual form.
            var alreadyInvoiced = await context.Invoices
                .AnyAsync(i => i.AppointmentId == appointmentId, cancellationToken);

            if (alreadyInvoiced)
            {
                logger.LogWarning(
                    "Invoice creation refused. Appointment {AppointmentId} already has an invoice.",
                    appointmentId);
                return InvoiceErrors.ExistingInvoiceForAppointment;
            }
        }
        else if (!user.IsUnrestrictedStaff())
        {
            // A standalone invoice has no owning doctor, so it stays outside a
            // doctor's reach (fail-closed), exactly as it is for reads.
            logger.LogWarning("Standalone invoice creation denied for user {UserId}.", user.Id);
            return ApplicationErrors.InvoiceAccessForbidden;
        }

        // 2. Standalone invoices stay a flat, immediately payable invoice. An
        //    appointment-derived invoice is built from its persisted charges
        //    instead, so the client's total is never trusted.
        var invoiceResult = Invoice.Create(request.PatientId, request.TotalAmount, request.AppointmentId);
        if (invoiceResult.IsError)
        {
            logger.LogWarning("Failed to create invoice for patient {PatientId}. Errors: {@Errors}", request.PatientId, invoiceResult.Errors);
            return invoiceResult.Errors;
        }

        var invoice = invoiceResult.Value;

        if (request.AppointmentId is { } sourceAppointmentId)
        {
            var appointment = await context.Appointments
                .Include(a => a.Service)
                .AsNoTracking()
                .FirstAsync(a => a.Id == sourceAppointmentId, cancellationToken);

            var treatments = await context.TreatmentRecords
                .AsNoTracking()
                .Where(t => t.AppointmentId == sourceAppointmentId)
                .ToListAsync(cancellationToken);

            var built = GenerateAppointmentInvoiceCommandHandler.BuildLines(appointment, treatments);
            if (built.IsError)
            {
                return built.Errors;
            }

            var (lines, _, _) = built.Value;
            if (lines.Count == 0)
            {
                logger.LogWarning(
                    "Invoice creation refused. Appointment {AppointmentId} has no billable items.",
                    sourceAppointmentId);
                return InvoiceErrors.NoBillableItems;
            }

            foreach (var line in lines)
            {
                invoice.AddLineImmediate(line.Description, line.Amount);
            }

            // The authoritative total comes from the persisted charges.
            invoice.RecalculateFromLines();
        }

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("invoice", cancellationToken);

        logger.LogInformation("Invoice created successfully with ID: {InvoiceId}", invoice.Id);

        return invoice.ToDto($"{patient.FirstName} {patient.LastName}");
    }
}
