using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;

public sealed class GetTreatmentPlanByIdQueryValidator : AbstractValidator<GetTreatmentPlanByIdQuery>
{
    public GetTreatmentPlanByIdQueryValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");
    }
}