using DentalClinic.Domain.Common.ValueObjects;
using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentRecords.Commands.UpdateTreatmentRecord;

public sealed class UpdateTreatmentRecordCommandValidator : AbstractValidator<UpdateTreatmentRecordCommand>
{
    public UpdateTreatmentRecordCommandValidator()
    {
        RuleFor(x => x.TreatmentRecordId)
            .NotEmpty().WithMessage("TreatmentRecordId is required.");

        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("DoctorId is required.");

        RuleFor(x => x.ToothNumber)
            .Must(FdiTooth.IsValidToothNumber)
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");

        RuleFor(x => x.ProcedureDetails)
            .NotEmpty().WithMessage("Procedure details are required.")
            .MaximumLength(1000).WithMessage("Procedure details must not exceed 1000 characters.");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");
    }
}