using FluentValidation;

namespace DentalClinic.Application.Features.Staff.Commands.SetStaffUserActive;

public sealed class SetStaffUserActiveCommandValidator : AbstractValidator<SetStaffUserActiveCommand>
{
    public SetStaffUserActiveCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}