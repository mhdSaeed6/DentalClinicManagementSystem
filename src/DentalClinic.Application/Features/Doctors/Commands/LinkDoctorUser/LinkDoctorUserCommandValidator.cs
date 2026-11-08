using FluentValidation;

namespace DentalClinic.Application.Features.Doctors.Commands.LinkDoctorUser;

public sealed class LinkDoctorUserCommandValidator : AbstractValidator<LinkDoctorUserCommand>
{
    public LinkDoctorUserCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty().WithMessage("DoctorId is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}