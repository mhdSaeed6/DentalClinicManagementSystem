using FluentValidation;

namespace DentalClinic.Application.Features.Patients.Queries.GetPatients;

public sealed class GetPatientsQueryValidator : AbstractValidator<GetPatientsQuery>
{
    public GetPatientsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        // Without an upper bound a client can request PageSize=1000000 and force
        // the server to materialise the whole patient table.
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");
    }
}