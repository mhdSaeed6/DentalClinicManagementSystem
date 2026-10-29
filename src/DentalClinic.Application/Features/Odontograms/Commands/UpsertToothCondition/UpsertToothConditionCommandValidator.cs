using DentalClinic.Domain.Common.ValueObjects;

using FluentValidation;

namespace DentalClinic.Application.Features.Odontograms.Commands.UpsertToothCondition;

public sealed class UpsertToothConditionCommandValidator : AbstractValidator<UpsertToothConditionCommand>
{
    public UpsertToothConditionCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.ToothNumber)
            .Must(FdiTooth.IsValidToothNumber)
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

        RuleFor(x => x.Condition)
            .Must(condition => Enum.IsDefined(condition))
            .WithMessage("Condition must be a defined tooth condition.");

        RuleFor(x => x.Surface)
            .Must(surface => surface is null || Enum.IsDefined(surface.Value))
            .WithMessage("Surface must be a defined tooth surface or omitted.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.");
    }
}