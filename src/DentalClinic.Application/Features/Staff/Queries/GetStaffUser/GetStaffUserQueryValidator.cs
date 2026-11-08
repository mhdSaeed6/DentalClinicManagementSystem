using FluentValidation;

namespace DentalClinic.Application.Features.Staff.Queries.GetStaffUser;

public sealed class GetStaffUserQueryValidator : AbstractValidator<GetStaffUserQuery>
{
    public GetStaffUserQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}