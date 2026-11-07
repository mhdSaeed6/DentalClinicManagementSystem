using FluentValidation;

namespace DentalClinic.Application.Features.LabCases.Queries.GetLabCases;

public sealed class GetLabCasesQueryValidator : AbstractValidator<GetLabCasesQuery>
{
    public GetLabCasesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");

        RuleFor(x => x.Status!)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("Status is not a valid lab-case status.");
    }
}