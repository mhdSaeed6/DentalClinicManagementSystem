using FluentValidation;

namespace DentalClinic.Application.Features.Staff.Queries.GetStaffUsers;

public sealed class GetStaffUsersQueryValidator : AbstractValidator<GetStaffUsersQuery>
{
    public GetStaffUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("PageNumber must be greater than zero.");
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100).WithMessage("PageSize must be between 1 and 100.");
    }
}