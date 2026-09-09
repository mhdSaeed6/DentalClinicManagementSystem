using FluentValidation;

namespace DentalClinic.Application.Features.Invoices.Queries.GetInvoiceById;

public sealed class GetInvoiceByIdValidator : AbstractValidator<GetInvoiceByIdQuery>
{
    public GetInvoiceByIdValidator()
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty()
            .WithErrorCode("InvoiceId.Required")
            .WithMessage("Invoice ID is required.");
    }
}
