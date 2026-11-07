using FluentValidation;

namespace DentalClinic.Application.Features.Doctors.Commands.UnlinkDoctorUser;

public sealed class UnlinkDoctorUserCommandValidator : AbstractValidator<UnlinkDoctorUserCommand>
{
    public UnlinkDoctorUserCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("DoctorId is required.");
    }
}