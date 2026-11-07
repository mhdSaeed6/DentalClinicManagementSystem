using FluentValidation;

namespace DentalClinic.Application.Features.LabCases.Queries.GetLabCaseById;

public sealed class GetLabCaseByIdQueryValidator : AbstractValidator<GetLabCaseByIdQuery>
{
    public GetLabCaseByIdQueryValidator()
    {
        RuleFor(x => x.LabCaseId)
            .NotEmpty().WithMessage("LabCaseId is required.");
    }
}