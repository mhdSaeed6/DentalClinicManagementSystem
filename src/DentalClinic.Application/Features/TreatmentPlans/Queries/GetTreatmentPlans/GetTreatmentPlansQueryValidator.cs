using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlans;

public sealed class GetTreatmentPlansQueryValidator : AbstractValidator<GetTreatmentPlansQuery>
{
    public GetTreatmentPlansQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");

        RuleFor(x => x.Status!)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("Status is not a valid treatment plan status.");
    }
}