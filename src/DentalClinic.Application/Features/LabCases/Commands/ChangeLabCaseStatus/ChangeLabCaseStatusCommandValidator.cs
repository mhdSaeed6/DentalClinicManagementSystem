using FluentValidation;

namespace DentalClinic.Application.Features.LabCases.Commands.ChangeLabCaseStatus;

public sealed class ChangeLabCaseStatusCommandValidator : AbstractValidator<ChangeLabCaseStatusCommand>
{
    public ChangeLabCaseStatusCommandValidator()
    {
        RuleFor(x => x.LabCaseId)
            .NotEmpty().WithMessage("LabCaseId is required.");

        // The workflow itself is enforced by the domain transition map; this only
        // rejects values that are not members of the enum.
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Status is not a valid lab-case status.");
    }
}