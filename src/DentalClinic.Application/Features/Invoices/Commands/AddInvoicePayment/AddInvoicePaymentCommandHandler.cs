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
        // 1. جلب الفاتورة فقط (دون الحاجة لـ Include الخاص بالـ Payments)
        var invoice = await context.Invoices
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
        await context.SaveChangesAsync(cancellationToken);

        // 6. تفريغ الـ Cache
        await cache.RemoveAsync($"invoice-{request.InvoiceId}", cancellationToken);
        await cache.RemoveByTagAsync("invoice", cancellationToken);

        return invoice.ToDto();
    }
}