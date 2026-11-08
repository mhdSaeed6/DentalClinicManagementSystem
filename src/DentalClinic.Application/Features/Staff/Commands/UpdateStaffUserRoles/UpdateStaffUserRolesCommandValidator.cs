using FluentValidation;

namespace DentalClinic.Application.Features.Staff.Commands.UpdateStaffUserRoles;

public sealed class UpdateStaffUserRolesCommandValidator : AbstractValidator<UpdateStaffUserRolesCommand>
{
    public UpdateStaffUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.Roles).NotEmpty().WithMessage("At least one role must be assigned.");
        RuleForEach(x => x.Roles).Must(StaffUsers.IsKnownRole).WithMessage("Only the Admin, Doctor and Receptionist roles can be assigned.");
    }
}