using FluentValidation;

namespace DentalClinic.Application.Features.Imports.Commands.PreviewPatientImport;

public sealed class PreviewPatientImportCommandValidator : AbstractValidator<PreviewPatientImportCommand>
{
    public PreviewPatientImportCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("The workbook file name is required.")
            .MaximumLength(260).WithMessage("The workbook file name is too long.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("The uploaded workbook is empty.");
    }
}