using FluentValidation;

namespace DentalClinic.Application.Features.DentalServices.Queries.GetDentalServiceById;

public sealed class GetDentalServiceByIdValidator : AbstractValidator<GetDentalServiceByIdQuery>
{
    public GetDentalServiceByIdValidator()
    {
        RuleFor(x => x.DentalServiceId)
            .NotEmpty().WithErrorCode("DentalServiceId.Required")
            .WithMessage("Dental service ID is required.");
    }
}
