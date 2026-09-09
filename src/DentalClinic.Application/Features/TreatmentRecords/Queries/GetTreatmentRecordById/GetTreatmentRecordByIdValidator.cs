using FluentValidation;

namespace DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecordById;

public sealed class GetTreatmentRecordByIdValidator : AbstractValidator<GetTreatmentRecordByIdQuery>
{
    public GetTreatmentRecordByIdValidator()
    {
        RuleFor(x => x.TreatmentRecordId)
            .NotEmpty()
            .WithErrorCode("TreatmentRecordId.Required")
            .WithMessage("Treatment record ID is required.");
    }
}
