using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentRecords.Commands.CreateTreatmentRecord;

public sealed class CreateTreatmentRecordCommandValidator : AbstractValidator<CreateTreatmentRecordCommand>
{
    public CreateTreatmentRecordCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("DoctorId is required.");

        RuleFor(x => x.ToothNumber)
            .Must(IsValidToothNumber)
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

        RuleFor(x => x.ProcedureDetails)
            .NotEmpty().WithMessage("Procedure details are required.")
            .MaximumLength(1000).WithMessage("Procedure details must not exceed 1000 characters.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");
    }

    private static bool IsValidToothNumber(int number)
    {
        bool isPermanent = (number >= 11 && number <= 18) ||
                            (number >= 21 && number <= 28) ||
                            (number >= 31 && number <= 38) ||
                            (number >= 41 && number <= 48);

        bool isPediatric = (number >= 51 && number <= 55) ||
                            (number >= 61 && number <= 65) ||
                            (number >= 71 && number <= 75) ||
                            (number >= 81 && number <= 85);

        return isPermanent || isPediatric;
    }
}