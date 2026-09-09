using FluentValidation;

namespace DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;

public sealed class GetAppointmentByIdValidator : AbstractValidator<GetAppointmentByIdQuery>
{
    public GetAppointmentByIdValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty()
            .WithErrorCode("AppointmentId.Required")
            .WithMessage("Appointment ID is required.");
    }
}
