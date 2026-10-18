using System.Data;

using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

public class AddInvoicePaymentCommandHandler(
    ILogger<AddInvoicePaymentCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<AddInvoicePaymentCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(AddInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        // The balance check below is read-modify-write, so the read and the write
        // must be one atomic unit. Under SERIALIZABLE the range lock taken by the
        // read is held until commit, so a competing payment either blocks or is
        // rejected by SQL Server rather than silently overpaying.
        await using var transaction = await context.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        // 1. جلب الفاتورة فقط (دون الحاجة لـ Include الخاص بالـ Payments)
        var invoice = await context.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogWarning("Invoice payment failed. Invoice {InvoiceId} not found.", request.InvoiceId);
            return ApplicationErrors.InvoiceNotFound;
        }

        // 2. تطبيق الـ Business Rules وتحديث الفاتورة
        var registerResult = invoice.RegisterPayment(request.Amount);
        if (registerResult.IsError)
        {
            logger.LogWarning("Failed to register payment for invoice {InvoiceId}. Errors: {@Errors}", request.InvoiceId, registerResult.Errors);
            return registerResult.Errors;
        }

        // 3. إنشاء كائن الـ Payment المستقل
        var paymentResult = Payment.Create(invoice.Id, request.Amount, request.Notes);
        if (paymentResult.IsError)
        {
            return paymentResult.Errors;
        }

        // 4. إضافة الـ Payment صراحة إلى جدول الـ Payments في قاعدة البيانات
        await context.Payments.AddAsync(paymentResult.Value, cancellationToken);

        // 5. حفظ التغييرات (تحديث الفاتورة + إضافة السطر الجديد في الدفعات)
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The invoice RowVersion no longer matches: another request paid or
            // edited this invoice while we held the stale copy. Surface a 409
            // instead of reporting a payment that was never persisted.
            logger.LogWarning(
                "Concurrent modification detected while registering a payment for invoice {InvoiceId}.",
                request.InvoiceId);

            return InvoiceErrors.ConcurrentModification;
        }

        // 6. تفريغ الـ Cache
        await cache.RemoveAsync($"invoice-{request.InvoiceId}", cancellationToken);
        await cache.RemoveByTagAsync("invoice", cancellationToken);

        var patientName = await context.Patients
            .Where(p => p.Id == invoice.PatientId)
            .Select(p => p.FirstName + " " + p.LastName)
            .FirstOrDefaultAsync(cancellationToken);

        return invoice.ToDto(patientName);
    }
}