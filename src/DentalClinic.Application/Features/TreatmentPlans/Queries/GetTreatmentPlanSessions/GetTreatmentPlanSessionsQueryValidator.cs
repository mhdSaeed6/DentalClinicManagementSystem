using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanSessions;

public sealed class GetTreatmentPlanSessionsQueryValidator : AbstractValidator<GetTreatmentPlanSessionsQuery>
{
    public GetTreatmentPlanSessionsQueryValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");
    }
}