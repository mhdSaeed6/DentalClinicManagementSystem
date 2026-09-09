using FluentValidation;

namespace DentalClinic.Application.Features.DentalServices.Queries.GetDentalServices;

public sealed class GetDentalServicesQueryValidator : AbstractValidator<GetDentalServicesQuery>
{
    public GetDentalServicesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");

        When(x => x.SearchTerm is not null, () =>
        {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).WithMessage("Search term must not exceed 100 characters.");
        });
    }
}
