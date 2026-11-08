using DentalClinic.Domain.Common.ValueObjects;

using FluentValidation;

namespace DentalClinic.Application.Features.LabCases.Commands.UpdateLabCase;

public sealed class UpdateLabCaseCommandValidator : AbstractValidator<UpdateLabCaseCommand>
{
    public UpdateLabCaseCommandValidator()
    {
        RuleFor(x => x.LabCaseId)
            .NotEmpty().WithMessage("LabCaseId is required.");

        // null مسموح — العمل غير مرتبط بسن محدد — لكن أي قيمة يجب أن تكون رقماً FDI صالحاً.
        RuleFor(x => x.ToothNumber)
            .Must(tooth => !tooth.HasValue || FdiTooth.IsValidToothNumber(tooth.Value))
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

        RuleFor(x => x.LabName)
            .NotEmpty().WithMessage("The dental lab name is required.")
            .MaximumLength(128).WithMessage("The dental lab name must not exceed 128 characters.");

        RuleFor(x => x.CaseType)
            .NotEmpty().WithMessage("The case type is required.")
            .MaximumLength(64).WithMessage("The case type must not exceed 64 characters.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Lab cost cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(512).WithMessage("Notes must not exceed 512 characters.");
    }
}