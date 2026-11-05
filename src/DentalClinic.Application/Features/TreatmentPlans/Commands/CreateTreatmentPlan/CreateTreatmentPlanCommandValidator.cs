using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;

public sealed class CreateTreatmentPlanCommandValidator : AbstractValidator<CreateTreatmentPlanCommand>
{
    public CreateTreatmentPlanCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");

        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("DoctorId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Treatment plan title is required.")
            .MaximumLength(200).WithMessage("Treatment plan title must not exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Treatment plan notes must not exceed 1000 characters.");
    }
}