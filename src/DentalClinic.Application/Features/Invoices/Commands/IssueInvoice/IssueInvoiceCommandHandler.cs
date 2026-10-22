using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Invoices.Commands.IssueInvoice;

public class IssueInvoiceCommandHandler(
    ILogger<IssueInvoiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache,
    IUser user)
    : IRequestHandler<IssueInvoiceCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(IssueInvoiceCommand request, CancellationToken cancellationToken)
    {
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

        var result = invoice.Issue();
        if (result.IsError)
        {
            logger.LogWarning("Failed to issue invoice {InvoiceId}. Errors: {@Errors}", request.InvoiceId, result.Errors);
            return result.Errors;
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