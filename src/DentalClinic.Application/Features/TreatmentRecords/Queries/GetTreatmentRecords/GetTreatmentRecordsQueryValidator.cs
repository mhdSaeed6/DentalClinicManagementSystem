using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecords;

public sealed class GetTreatmentRecordsQueryValidator : AbstractValidator<GetTreatmentRecordsQuery>
{
    public GetTreatmentRecordsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than zero.")
            .LessThanOrEqualTo(100).WithMessage("Page size must not exceed 100.");
    }
}
