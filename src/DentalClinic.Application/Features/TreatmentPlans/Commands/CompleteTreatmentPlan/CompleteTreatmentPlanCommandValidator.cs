using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CompleteTreatmentPlan;

public sealed class CompleteTreatmentPlanCommandValidator : AbstractValidator<CompleteTreatmentPlanCommand>
{
    public CompleteTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");
    }
}