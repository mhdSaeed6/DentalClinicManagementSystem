using DentalClinic.Domain.Common.ValueObjects;

using FluentValidation;

namespace DentalClinic.Application.Features.Odontograms.Commands.RemoveToothCondition;

public sealed class RemoveToothConditionCommandValidator : AbstractValidator<RemoveToothConditionCommand>
{
    public RemoveToothConditionCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.ToothNumber)
            .Must(FdiTooth.IsValidToothNumber)
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");
    }
}