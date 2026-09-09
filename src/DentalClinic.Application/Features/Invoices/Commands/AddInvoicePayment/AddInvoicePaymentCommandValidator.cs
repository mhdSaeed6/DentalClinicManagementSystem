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

        When(x => !string.IsNullOrWhiteSpace(x.Notes), () =>
        {
            RuleFor(x => x.Notes)
                .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
        });
    }
}
