using FluentValidation;

namespace DentalClinic.Application.Features.Doctors.Queries.GetDoctorById;

public sealed class GetDoctorByIdValidator : AbstractValidator<GetDoctorByIdQuery>
{
    public GetDoctorByIdValidator()
    {
        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .WithErrorCode("DoctorId.Required")
            .WithMessage("Doctor ID is required.");
    }
}
