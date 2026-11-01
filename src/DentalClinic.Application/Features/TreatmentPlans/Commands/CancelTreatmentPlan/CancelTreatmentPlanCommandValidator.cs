using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlan;

public sealed class CancelTreatmentPlanCommandValidator : AbstractValidator<CancelTreatmentPlanCommand>
{
    public CancelTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");
    }
}