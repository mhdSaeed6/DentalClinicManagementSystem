using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.ActivateTreatmentPlan;

public sealed class ActivateTreatmentPlanCommandValidator : AbstractValidator<ActivateTreatmentPlanCommand>
{
    public ActivateTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");
    }
}