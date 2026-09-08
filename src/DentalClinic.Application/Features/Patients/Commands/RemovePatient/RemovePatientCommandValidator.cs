using FluentValidation;

namespace DentalClinic.Application.Features.Patients.Commands.RemovePatient;

public class RemovePatientCommandValidator : AbstractValidator<RemovePatientCommand>
{
    public RemovePatientCommandValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("PatientId is required.");
    }
}