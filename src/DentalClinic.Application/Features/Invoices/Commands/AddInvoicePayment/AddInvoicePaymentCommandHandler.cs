using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;

public class AddInvoicePaymentCommandHandler(
    ILogger<AddInvoicePaymentCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<AddInvoicePaymentCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(AddInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await context.Invoices.FindAsync([request.InvoiceId], cancellationToken);
        if (invoice is null)
        {
            logger.LogWarning("Invoice payment failed. Invoice {InvoiceId} not found.", request.InvoiceId);
            return ApplicationErrors.InvoiceNotFound;
        }

        var result = invoice.AddPayment(request.Amount, request.Notes);
        if (result.IsError)
        {
            logger.LogWarning("Failed to add payment to invoice {InvoiceId}. Errors: {@Errors}", request.InvoiceId, result.Errors);
            return result.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"invoice-{request.InvoiceId}", cancellationToken);
        await cache.RemoveByTagAsync("invoice", cancellationToken);

        return invoice.ToDto();
    }
}
