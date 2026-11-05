using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlan;

public sealed class UpdateTreatmentPlanCommandValidator : AbstractValidator<UpdateTreatmentPlanCommand>
{
    public UpdateTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Treatment plan title is required.")
            .MaximumLength(200).WithMessage("Treatment plan title must not exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Treatment plan notes must not exceed 1000 characters.");
    }
}