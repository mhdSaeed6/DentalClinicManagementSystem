using FluentValidation;

namespace DentalClinic.Application.Features.Imports.Commands.CommitPatientImport;

public sealed class CommitPatientImportCommandValidator : AbstractValidator<CommitPatientImportCommand>
{
    public CommitPatientImportCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("The workbook file name is required.")
            .MaximumLength(260).WithMessage("The workbook file name is too long.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("The uploaded workbook is empty.");
    }
}