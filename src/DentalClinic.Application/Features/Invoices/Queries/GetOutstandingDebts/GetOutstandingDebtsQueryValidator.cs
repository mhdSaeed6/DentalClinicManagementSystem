using FluentValidation;

namespace DentalClinic.Application.Features.Invoices.Queries.GetOutstandingDebts;

public sealed class GetOutstandingDebtsQueryValidator : AbstractValidator<GetOutstandingDebtsQuery>
{
    public GetOutstandingDebtsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");

        RuleFor(x => x.MinAmount)
            .GreaterThan(0).WithMessage("The minimum amount must be positive.")
            .When(x => x.MinAmount.HasValue);
    }
}
