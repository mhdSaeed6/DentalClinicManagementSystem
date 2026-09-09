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
            .InclusiveBetween(11, 85).WithMessage("Tooth number must be a valid dental chart number.");

        RuleFor(x => x.ProcedureDetails)
            .NotEmpty().WithMessage("Procedure details are required.")
            .MaximumLength(1000).WithMessage("Procedure details must not exceed 1000 characters.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");
    }
}
