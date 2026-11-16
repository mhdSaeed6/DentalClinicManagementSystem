using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Invoices.Enums;

using MediatR;

namespace DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;

/// <summary>
/// تسجيل دفعة على فاتورة. <see cref="Method"/> و<see cref="Reference"/> بيانات
/// وصفية فقط: لا تدخل في حساب المدفوع أو المتبقي أو المستحقات.
/// القيمتان الافتراضيتان تحافظان على توافق أي نداء قديم (نقداً، بلا مرجع).
/// </summary>
public sealed record AddInvoicePaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string? Notes,
    PaymentMethod Method = PaymentMethod.Cash,
    string? Reference = null) : IRequest<Result<InvoiceDto>>;