using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.PerformTreatmentPlanItem;

public sealed class PerformTreatmentPlanItemCommandValidator : AbstractValidator<PerformTreatmentPlanItemCommand>
{
    public PerformTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("ItemId is required.");

        RuleFor(x => x.ActualCost)
            .GreaterThanOrEqualTo(0).WithMessage("Actual cost cannot be negative.");
    }
}