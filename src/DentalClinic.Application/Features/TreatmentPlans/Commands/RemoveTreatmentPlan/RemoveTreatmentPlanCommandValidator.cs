using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlan;

public sealed class RemoveTreatmentPlanCommandValidator : AbstractValidator<RemoveTreatmentPlanCommand>
{
    public RemoveTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");
    }
}