using DentalClinic.Domain.Common.ValueObjects;
using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlanItem;

public sealed class UpdateTreatmentPlanItemCommandValidator : AbstractValidator<UpdateTreatmentPlanItemCommand>
{
    public UpdateTreatmentPlanItemCommandValidator()
    {
        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");

        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("ItemId is required.");

        RuleFor(x => x.ToothNumber)
            .Must(tooth => !tooth.HasValue || FdiTooth.IsValidToothNumber(tooth.Value))
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

        RuleFor(x => x.ProcedureDetails)
            .NotEmpty().WithMessage("Procedure details are required.")
            .MaximumLength(1000).WithMessage("Procedure details must not exceed 1000 characters.");

        RuleFor(x => x.EstimatedCost)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated cost cannot be negative.");

        RuleFor(x => x.SessionNumber)
            .GreaterThan(0).WithMessage("Session number must be greater than zero.");
    }
}