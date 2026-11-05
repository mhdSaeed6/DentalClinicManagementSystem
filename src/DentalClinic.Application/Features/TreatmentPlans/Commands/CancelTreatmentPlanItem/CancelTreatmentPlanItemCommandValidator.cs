using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlanItem;

public sealed class CancelTreatmentPlanItemCommandValidator : AbstractValidator<CancelTreatmentPlanItemCommand>
{
    public CancelTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("ItemId is required.");
    }
}