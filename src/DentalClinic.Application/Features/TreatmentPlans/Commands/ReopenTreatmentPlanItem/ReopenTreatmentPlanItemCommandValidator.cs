using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.ReopenTreatmentPlanItem;

public sealed class ReopenTreatmentPlanItemCommandValidator : AbstractValidator<ReopenTreatmentPlanItemCommand>
{
    public ReopenTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("ItemId is required.");
    }
}