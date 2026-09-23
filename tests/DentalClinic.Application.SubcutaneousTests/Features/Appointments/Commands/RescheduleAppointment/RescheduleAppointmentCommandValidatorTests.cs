using DentalClinic.Application.Features.Appointments.Commands.RescheduleAppointment;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.RescheduleAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RescheduleAppointmentCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task RescheduleAppointment_ShouldFailValidation_WhenAppointmentIdIsEmpty()
    {
        var command = CreateValidCommand() with { AppointmentId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task RescheduleAppointment_ShouldFailValidation_WhenScheduledDateTimeIsInPast()
    {
        var command = CreateValidCommand() with { ScheduledDateTime = DateTime.UtcNow.AddMinutes(-10) };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public async Task RescheduleAppointment_ShouldFailValidation_WhenDurationInMinutesIsZeroOrNegative(int duration)
    {
        var command = CreateValidCommand() with { DurationInMinutes = duration };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    private static RescheduleAppointmentCommand CreateValidCommand() => new(
        AppointmentId: Guid.NewGuid(),
        ScheduledDateTime: DateTime.UtcNow.AddDays(1),
        DurationInMinutes: 30);
}