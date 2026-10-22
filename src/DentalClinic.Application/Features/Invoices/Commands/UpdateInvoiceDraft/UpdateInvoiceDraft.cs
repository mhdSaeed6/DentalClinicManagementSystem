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

namespace DentalClinic.Application.Features.Invoices.Commands.UpdateInvoiceDraft;

public record UpdateInvoiceLine(Guid LineId, string Description, decimal Amount);

/// <summary>
/// Edits a Draft invoice only. The client never supplies a total: lines are
/// validated and summed on the server. Editing an issued invoice is rejected by
/// the domain, so a late-arriving request cannot mutate a settled invoice.
/// </summary>
public record UpdateInvoiceDraftCommand(
    Guid InvoiceId,
    IReadOnlyList<UpdateInvoiceLine> Lines)
    : IRequest<Result<InvoiceDto>>;

public class UpdateInvoiceDraftCommandHandler(
    ILogger<UpdateInvoiceDraftCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<UpdateInvoiceDraftCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(UpdateInvoiceDraftCommand request, CancellationToken cancellationToken)
    {
        // Only Lines are loaded: an invoice being edited or refreshed is a Draft, and a
        // Draft can never carry payments. Avoiding the second collection include
        // keeps this a single, unambiguous query.
        var invoice = await context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return ApplicationErrors.InvoiceNotFound;
        }

        // Ownership: a doctor may only edit an invoice linked to one of their
        // own appointments. The check runs before the state check so a foreign
        // invoice never reveals whether it could have been edited.
        if (!await user.CanAccessInvoiceAsync(context, invoice, cancellationToken))
        {
            logger.LogWarning("Draft edit denied for user {UserId} on invoice {InvoiceId}.", user.Id, request.InvoiceId);
            return ApplicationErrors.InvoiceAccessForbidden;
        }

        // Check the invoice state FIRST so editing an issued or paid invoice reports
        // the real reason (Conflict) rather than a misleading line-not-found.
        var editable = invoice.EnsureEditable();
        if (editable.IsError)
        {
            logger.LogWarning("Draft edit refused for invoice {InvoiceId}. Errors: {@Errors}", request.InvoiceId, editable.Errors);
            return editable.Errors;
        }

        if (request.Lines.Count == 0)
        {
            return InvoiceErrors.NoBillableItems;
        }

        if (request.Lines.Any(l => l.Amount <= 0 || string.IsNullOrWhiteSpace(l.Description)))
        {
            return InvoiceErrors.InvalidLineAmount;
        }

        var applied = invoice.ApplyLineChanges(
            [.. request.Lines.Select(l => ((Guid?)l.LineId, l.Description, l.Amount))]);

        if (applied.IsError)
        {
            return applied.Errors;
        }

        // Persist row-count changes explicitly rather than relying on EF orphan
        // detection, which does not reliably delete removed children here.
        var (added, removed) = applied.Value;

        if (added.Count > 0)
        {
            context.InvoiceLines.AddRange(added);
        }

        if (removed.Count > 0)
        {
            context.InvoiceLines.RemoveRange(removed);
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"invoice-{request.InvoiceId}", cancellationToken);
        await cache.RemoveByTagAsync("invoice", cancellationToken);

        var patientName = await context.Patients
            .Where(p => p.Id == invoice.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        return invoice.ToDto(patientName);
    }
}

/// <summary>
/// Recalculates a Draft from its source appointment. This is the only way a
/// draft's total follows a change in the appointment or its treatments; nothing
/// refreshes implicitly. When the draft already carries manual adjustments the
/// caller must explicitly acknowledge that they will be discarded.
/// </summary>
public record RefreshInvoiceDraftCommand(
    Guid InvoiceId,
    bool ConfirmDiscardManualAdjustments = false)
    : IRequest<Result<InvoiceDto>>;

public class RefreshInvoiceDraftCommandHandler(
    ILogger<RefreshInvoiceDraftCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<RefreshInvoiceDraftCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(RefreshInvoiceDraftCommand request, CancellationToken cancellationToken)
    {
        // Only Lines are loaded: an invoice being edited or refreshed is a Draft, and a
        // Draft can never carry payments. Avoiding the second collection include
        // keeps this a single, unambiguous query.
        var invoice = await context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return ApplicationErrors.InvoiceNotFound;
        }

        // Ownership: a doctor may only refresh an invoice linked to one of their
        // own appointments. The check runs before the state checks so a foreign
        // invoice never reveals whether it could have been refreshed.
        if (!await user.CanAccessInvoiceAsync(context, invoice, cancellationToken))
        {
            logger.LogWarning("Draft refresh denied for user {UserId} on invoice {InvoiceId}.", user.Id, request.InvoiceId);
            return ApplicationErrors.InvoiceAccessForbidden;
        }

        if (invoice.AppointmentId is not { } appointmentId)
        {
            return InvoiceErrors.DraftHasNoSourceAppointment;
        }

        if (invoice.HasManualAdjustments && !request.ConfirmDiscardManualAdjustments)
        {
            logger.LogWarning(
                "Refresh of invoice {InvoiceId} refused because it has manual adjustments and was not confirmed.",
                request.InvoiceId);
            return InvoiceErrors.ManualAdjustmentsWouldBeLost;
        }

        var appointment = await context.Appointments
            .Include(a => a.Service)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return ApplicationErrors.AppointmentNotFound;
        }

        var treatments = await context.TreatmentRecords
            .AsNoTracking()
            .Where(t => t.AppointmentId == appointment.Id)
            .ToListAsync(cancellationToken);

        var built = GenerateAppointmentInvoiceCommandHandler.BuildLines(appointment, treatments);
        if (built.IsError)
        {
            return built.Errors;
        }

        var (lines, _, _) = built.Value;
        if (lines.Count == 0)
        {
            return InvoiceErrors.NoBillableItems;
        }

        var synced = invoice.ApplyLineChanges(
            [.. lines.Select(l => ((Guid?)null, l.Description, l.Amount))]);

        if (synced.IsError)
        {
            return synced.Errors;
        }

        // Persist row-count changes explicitly rather than relying on EF orphan
        // detection, which does not reliably delete removed children here.
        var (added, removed) = synced.Value;

        if (added.Count > 0)
        {
            context.InvoiceLines.AddRange(added);
        }

        if (removed.Count > 0)
        {
            context.InvoiceLines.RemoveRange(removed);
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"invoice-{request.InvoiceId}", cancellationToken);
        await cache.RemoveByTagAsync("invoice", cancellationToken);

        var patientName = await context.Patients
            .Where(p => p.Id == invoice.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        return invoice.ToDto(patientName);
    }
}