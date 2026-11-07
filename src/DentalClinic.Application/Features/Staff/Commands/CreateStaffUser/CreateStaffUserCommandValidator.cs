using FluentValidation;

namespace DentalClinic.Application.Features.Staff.Commands.CreateStaffUser;

public sealed class CreateStaffUserCommandValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12).WithMessage("The password must be at least 12 characters long.");
        RuleFor(x => x.Roles).NotEmpty().WithMessage("At least one role must be assigned.");
        RuleForEach(x => x.Roles).Must(StaffUsers.IsKnownRole).WithMessage("Only the Admin, Doctor and Receptionist roles can be assigned.");
    }
}