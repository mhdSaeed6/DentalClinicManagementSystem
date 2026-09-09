using FluentValidation;

namespace DentalClinic.Application.Features.Doctors.Commands.RemoveDoctor;

public sealed class RemoveDoctorCommandValidator : AbstractValidator<RemoveDoctorCommand>
{
    public RemoveDoctorCommandValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("DoctorId is required.");
    }
}
