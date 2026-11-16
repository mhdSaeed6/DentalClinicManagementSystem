using DentalClinic.Domain.Invoices;

using FluentValidation;

namespace DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;

public sealed class AddInvoicePaymentCommandValidator : AbstractValidator<AddInvoicePaymentCommand>
{
    public AddInvoicePaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty().WithMessage("InvoiceId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Method)
            .IsInEnum().WithMessage("Payment method is not recognized.");

        When(x => !string.IsNullOrWhiteSpace(x.Notes), () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
        });

        // المرجع اختياري لكل الطرق (نقداً قد يحمل رقم إيصال)، لكن طوله محدود
        // لأن العمود nvarchar(64).
        When(x => !string.IsNullOrWhiteSpace(x.Reference), () =>
        {
            RuleFor(x => x.Reference)
                .MaximumLength(Payment.MaxReferenceLength)
                .WithMessage($"Reference must not exceed {Payment.MaxReferenceLength} characters.");
        });
    }
}