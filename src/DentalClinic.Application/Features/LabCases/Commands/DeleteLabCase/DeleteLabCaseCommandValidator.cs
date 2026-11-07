using FluentValidation;

namespace DentalClinic.Application.Features.LabCases.Commands.DeleteLabCase;

public sealed class DeleteLabCaseCommandValidator : AbstractValidator<DeleteLabCaseCommand>
{
    public DeleteLabCaseCommandValidator()
    {
        RuleFor(x => x.LabCaseId)
            .NotEmpty().WithMessage("LabCaseId is required.");
    }
}