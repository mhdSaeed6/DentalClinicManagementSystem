using DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.CreateAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateAppointmentCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task CreateAppointment_ShouldFailValidation_WhenPatientIdIsEmpty()
    {
        var command = CreateValidCommand() with { PatientId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateAppointment_ShouldFailValidation_WhenDoctorIdIsEmpty()
    {
        var command = CreateValidCommand() with { DoctorId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateAppointment_ShouldFailValidation_WhenServiceIdIsEmpty()
    {
        var command = CreateValidCommand() with { ServiceId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateAppointment_ShouldFailValidation_WhenScheduledDateTimeIsInPast()
    {
        var command = CreateValidCommand() with { ScheduledDateTime = DateTime.UtcNow.AddMinutes(-10) };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public async Task CreateAppointment_ShouldFailValidation_WhenDurationInMinutesIsZeroOrNegative(int duration)
    {
        var command = CreateValidCommand() with { DurationInMinutes = duration };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateAppointment_ShouldFailValidation_WhenNotesExceedMaximumLength()
    {
        var longNotes = new string('A', 1001);
        var command = CreateValidCommand() with { Notes = longNotes };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    private static CreateAppointmentCommand CreateValidCommand() => new(
        PatientId: Guid.NewGuid(),
        DoctorId: Guid.NewGuid(),
        ServiceId: Guid.NewGuid(),
        ScheduledDateTime: DateTime.UtcNow.AddHours(2),
        DurationInMinutes: 45,
        Notes: "Valid Note");
}