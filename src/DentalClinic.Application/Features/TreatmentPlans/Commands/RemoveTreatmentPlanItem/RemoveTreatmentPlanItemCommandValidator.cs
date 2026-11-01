using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlanItem;

public sealed class RemoveTreatmentPlanItemCommandValidator : AbstractValidator<RemoveTreatmentPlanItemCommand>
{
    public RemoveTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("ItemId is required.");
    }
}