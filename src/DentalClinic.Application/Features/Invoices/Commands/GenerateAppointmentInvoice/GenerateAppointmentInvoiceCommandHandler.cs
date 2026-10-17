using System.Data;

using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Appointments;
using DentalClinic.Domain.Appointments.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using DentalClinic.Domain.TreatmentRecords;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;

public class GenerateAppointmentInvoiceCommandHandler(
    ILogger<GenerateAppointmentInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<GenerateAppointmentInvoiceCommand, Result<GenerateAppointmentInvoiceResult>>
{
    public async Task<Result<GenerateAppointmentInvoiceResult>> Handle(
        GenerateAppointmentInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating invoice draft for appointment {AppointmentId}", request.AppointmentId);

        // 1. The appointment and its selected service are the single source of
        //    truth for this operation; nothing is taken from the caller.
        var appointment = await context.Appointments
            .Include(a => a.Service)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken);

        if (appointment is null)
        {
            logger.LogWarning("Invoice generation failed. Appointment {AppointmentId} not found.", request.AppointmentId);
            return ApplicationErrors.AppointmentNotFound;
        }

        if (!user.CanAccessAppointment(appointment.DoctorId))
        {
            logger.LogWarning("Invoice generation denied for user {UserId} on appointment {AppointmentId}.", user.Id, request.AppointmentId);
            return ApplicationErrors.AppointmentAccessForbidden;
        }

        // 2. Billing a visit that never happened would be an incorrect invoice.
        if (appointment.Status != AppointmentStatus.Completed)
        {
            logger.LogWarning("Invoice generation refused. Appointment {AppointmentId} is {Status}.", request.AppointmentId, appointment.Status);
            return InvoiceErrors.AppointmentNotCompleted;
        }

        // 3. Only this appointment's treatments are considered; another visit's
        //    or patient's records can never leak into this invoice.
        var treatments = await context.TreatmentRecords
            .AsNoTracking()
            .Where(t => t.AppointmentId == appointment.Id)
            .ToListAsync(cancellationToken);

        var buildResult = BuildLines(appointment, treatments);
        if (buildResult.IsError)
        {
            logger.LogWarning(
                "Invoice generation failed for appointment {AppointmentId}. Errors: {@Errors}",
                request.AppointmentId,
                buildResult.Errors);
            return buildResult.Errors;
        }

        var (lines, serviceSubtotal, treatmentSubtotal) = buildResult.Value;

        if (lines.Count == 0)
        {
            // Consultation-only visit with nothing billable: never create an
            // empty invoice.
            logger.LogWarning("Invoice generation refused. Appointment {AppointmentId} has no billable items.", request.AppointmentId);
            return InvoiceErrors.NoBillableItems;
        }

        var totalAmount = serviceSubtotal + treatmentSubtotal;

        // 4. Check-then-insert runs inside one serializable transaction so two
        //    simultaneous requests cannot both create a draft for the same visit.
        await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var existing = await context.Invoices
            .AsNoTracking()
            .Where(i => i.AppointmentId == appointment.Id)
            .ToListAsync(cancellationToken);

        // An existing draft (unpaid, zero payments) created for this appointment is reusable.
        // Only reuse invoices that are still Draft lifecycle and untouched.
        var reusableDraft = existing.FirstOrDefault(i =>
            i.LifecycleStatus == InvoiceLifecycleStatus.Draft &&
            i.Status == PaymentStatus.Unpaid &&
            i.PaidAmount == 0);

        if (reusableDraft is not null)
        {
            logger.LogInformation(
                "Invoice draft already exists for appointment {AppointmentId}. Returning invoice {InvoiceId}.",
                request.AppointmentId,
                reusableDraft.Id);

            await transaction.CommitAsync(cancellationToken);

            return await BuildResultAsync(reusableDraft, serviceSubtotal, treatmentSubtotal, lines, reusedExisting: true, cancellationToken);
        }

        // Anything else linked to this visit has payments recorded or is issued; regenerating
        // would silently modify it, so it is refused.
        if (existing.Count > 0)
        {
            logger.LogWarning("Invoice generation refused. Appointment {AppointmentId} already has an existing invoice.", request.AppointmentId);
            return InvoiceErrors.ExistingInvoiceForAppointment;
        }

        // 5. The total is persisted on the invoice as a price snapshot, so later
        //    catalog or treatment edits never rewrite an already generated invoice.
        var invoiceResult = Invoice.Create(appointment.PatientId, totalAmount, appointment.Id, InvoiceLifecycleStatus.Draft);
        if (invoiceResult.IsError)
        {
            logger.LogWarning("Invoice generation failed for appointment {AppointmentId}. Errors: {@Errors}", request.AppointmentId, invoiceResult.Errors);
            return invoiceResult.Errors;
        }

        var invoice = invoiceResult.Value;

        foreach (var line in lines)
        {
            invoice.AddLineImmediate(line.Description, line.Amount);
        }

        // The total is derived from the persisted lines, never from the request.
        invoice.RecalculateFromLines();

        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RemoveByTagAsync("invoice", cancellationToken);

        logger.LogInformation("Invoice {InvoiceId} generated for appointment {AppointmentId}.", invoice.Id, request.AppointmentId);

        return await BuildResultAsync(invoice, serviceSubtotal, treatmentSubtotal, lines, reusedExisting: false, cancellationToken);
    }

    /// <summary>
    /// Builds the billable lines for the visit: the appointment's service charge
    /// (when a service and a valid positive price exist) plus each treatment that
    /// carries a positive cost. A missing or soft-deleted service is reported as
    /// an error instead of silently billing without it.
    /// </summary>
    internal static Result<(List<InvoiceLineDto> Lines, decimal ServiceSubtotal, decimal TreatmentSubtotal)> BuildLines(
        Appointment appointment,
        IReadOnlyList<TreatmentRecord> treatments)
    {
        var lines = new List<InvoiceLineDto>(treatments.Count + 1);
        decimal serviceSubtotal = 0;
        decimal treatmentSubtotal = 0;

        if (appointment.ServiceId is not null)
        {
            var service = appointment.Service;
            if (service is null)
            {
                // The reference points at a service that no longer resolves, so
                // its price cannot be trusted. Explain instead of guessing.
                return ApplicationErrors.ServiceNotFound;
            }

            if (service.Price > 0)
            {
                lines.Add(new InvoiceLineDto(Guid.Empty, service.Name, service.Price));
                serviceSubtotal = service.Price;
            }
        }

        foreach (var treatment in treatments)
        {
            if (treatment.Cost <= 0)
            {
                continue;
            }

            lines.Add(new InvoiceLineDto(
                Guid.Empty,
                $"{treatment.ProcedureDetails} (Tooth {treatment.ToothNumber})",
                treatment.Cost));
            treatmentSubtotal += treatment.Cost;
        }

        return (lines, serviceSubtotal, treatmentSubtotal);
    }

    private async Task<GenerateAppointmentInvoiceResult> BuildResultAsync(
        Invoice invoice,
        decimal serviceSubtotal,
        decimal treatmentSubtotal,
        IReadOnlyCollection<InvoiceLineDto> lines,
        bool reusedExisting,
        CancellationToken cancellationToken)
    {
        var patientName = await context.Patients
            .Where(p => p.Id == invoice.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        // A reused draft keeps its own persisted total; breakdown is only reported for the newly created one.
        return new GenerateAppointmentInvoiceResult(
            invoice.Id,
            invoice.PatientId,
            patientName ?? string.Empty,
            invoice.AppointmentId ?? Guid.Empty,
            reusedExisting ? 0 : serviceSubtotal,
            reusedExisting ? 0 : treatmentSubtotal,
            invoice.TotalAmount,
            invoice.Status,
            invoice.LifecycleStatus,
            reusedExisting,
            reusedExisting ? [] : lines);
    }
}
