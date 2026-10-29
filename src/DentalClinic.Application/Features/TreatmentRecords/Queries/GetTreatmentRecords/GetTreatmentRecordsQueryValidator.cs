using DentalClinic.Domain.Common.ValueObjects;

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

        RuleFor(x => x.ToothNumber)
            .Must(tooth => FdiTooth.IsValidToothNumber(tooth!.Value))
            .When(x => x.ToothNumber.HasValue)
            .WithMessage("Tooth number must be a valid FDI dental chart number (e.g., 11-18, 21-28, 51-55, 71-75).");
    }
}
