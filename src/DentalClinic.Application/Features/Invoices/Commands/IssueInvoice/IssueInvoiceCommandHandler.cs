using System.Data;

using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;
using DentalClinic.Domain.Invoices.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Invoices.Commands.IssueInvoice;

public class IssueInvoiceCommandHandler(
    ILogger<IssueInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user,
    IInvoiceNumberGenerator invoiceNumbers,
    TimeProvider timeProvider)
    : IRequestHandler<IssueInvoiceCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Serialisable: the number reservation, the draft→issued transition and
        // the save must be one unit. Without it a concurrent issue could read the
        // same draft, take a second number and only fail on the rowversion, which
        // is a wasted number and a confusing 409.
        await using var transaction = await context.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        // Reserve the number FIRST. It is the only contended lock in this flow,
        // so taking it before reading the invoice keeps every issuer queued on a
        // single resource. Reading the invoice first would hold a shared lock on
        // it while waiting for the counter, and the winner's UPDATE would then
        // block on that shared lock — a lock-upgrade deadlock. A reservation made
        // for a request that ends early is released by the rollback below.
        var invoiceNumber = await invoiceNumbers.NextAsync(cancellationToken);

        // Only Lines are needed to validate contents and recompute the total. Issuance is
        // only valid from a Draft, which by definition has no payments yet.
        var invoice = await context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogWarning("Issue invoice failed. Invoice {InvoiceId} not found.", request.InvoiceId);
            return ApplicationErrors.InvoiceNotFound;
        }

        // Ownership: a doctor may only issue an invoice linked to one of their
        // own appointments. The check runs before the state check so a foreign
        // invoice never reveals whether it could have been issued.
        if (!await user.CanAccessInvoiceAsync(context, invoice, cancellationToken))
        {
            logger.LogWarning("Invoice issuance denied for user {UserId} on invoice {InvoiceId}.", user.Id, request.InvoiceId);
            return ApplicationErrors.InvoiceAccessForbidden;
        }

        // Issuance is idempotent: a repeated request must return the number the
        // invoice already has. Rolling back (rather than committing) releases the
        // reservation made above, so a repeat does not burn a number.
        if (invoice.LifecycleStatus == InvoiceLifecycleStatus.Issued)
        {
            await transaction.RollbackAsync(cancellationToken);
            return invoice.ToDto(await ResolvePatientNameAsync(invoice.PatientId, cancellationToken));
        }

        var result = invoice.Issue(invoiceNumber, timeProvider.GetUtcNow());
        if (result.IsError)
        {
            logger.LogWarning("Failed to issue invoice {InvoiceId}. Errors: {@Errors}", request.InvoiceId, result.Errors);
            return result.Errors;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed the invoice between our read and our write.
            // The transaction (and the reserved number) rolls back.
            logger.LogWarning(
                "Invoice {InvoiceId} was modified concurrently while being issued.",
                request.InvoiceId);

            return InvoiceErrors.ConcurrentModification;
        }

        await cache.RemoveAsync($"invoice-{request.InvoiceId}", cancellationToken);
        await cache.RemoveByTagAsync("invoice", cancellationToken);

        logger.LogInformation(
            "Invoice {InvoiceId} issued as {InvoiceNumber}.",
            request.InvoiceId,
            invoice.InvoiceNumber);

        return invoice.ToDto(await ResolvePatientNameAsync(invoice.PatientId, cancellationToken));
    }

    private async Task<string?> ResolvePatientNameAsync(Guid patientId, CancellationToken cancellationToken)
    {
        return await context.Patients
            .Where(p => p.Id == patientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);
    }
}