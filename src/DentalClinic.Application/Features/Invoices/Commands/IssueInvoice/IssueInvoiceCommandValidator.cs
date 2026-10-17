using FluentValidation;

namespace DentalClinic.Application.Features.Invoices.Commands.IssueInvoice;

public class IssueInvoiceCommandValidator : AbstractValidator<IssueInvoiceCommand>
{
    public IssueInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty();
    }
}