using FluentValidation;

namespace DentalClinic.Application.Features.Staff.Commands.ResetStaffUserPassword;

public sealed class ResetStaffUserPasswordCommandValidator : AbstractValidator<ResetStaffUserPasswordCommand>
{
    public ResetStaffUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(12).WithMessage("The password must be at least 12 characters long.");
    }
}