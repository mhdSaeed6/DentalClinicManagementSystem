using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.CompleteAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CompleteAppointmentCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task CompleteAppointment_ShouldFailValidation_WhenAppointmentIdIsEmpty()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(Guid.Empty);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("AppointmentId", result.TopError.Code);
    }

    [Fact]
    public async Task CompleteAppointment_ShouldPassValidation_WhenNoTreatmentsProvided()
    {
        // Arrange – an unknown id fails later with AppointmentNotFound, proving the
        // command itself (with no treatments) passed validation.
        var command = new CompleteAppointmentCommand(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("Appointment.NotFound", result.TopError.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(19)]
    [InlineData(99)]
    public async Task CompleteAppointment_ShouldFailValidation_WhenTreatmentToothNumberIsInvalid(int toothNumber)
    {
        // Arrange
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
            [new CompleteAppointmentTreatment(toothNumber, "Composite filling", 10m)]);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("ToothNumber", result.TopError.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CompleteAppointment_ShouldFailValidation_WhenTreatmentProcedureDetailsIsEmpty(string procedureDetails)
    {
        // Arrange
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
            [new CompleteAppointmentTreatment(11, procedureDetails, 10m)]);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("ProcedureDetails", result.TopError.Code);
    }

    [Fact]
    public async Task CompleteAppointment_ShouldFailValidation_WhenTreatmentCostIsNegative()
    {
        // Arrange
        var command = new CompleteAppointmentCommand(
            Guid.NewGuid(),
            [new CompleteAppointmentTreatment(11, "Composite filling", -1m)]);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("Cost", result.TopError.Code);
    }

    [Fact]
    public void CompleteAppointment_ShouldPassValidation_WhenServiceIdIsNull()
    {
        // Replacing the service is optional; omitting it is valid.
        var validator = new CompleteAppointmentCommandValidator();
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), null, null);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CompleteAppointment_ShouldPassValidation_WhenServiceIdIsProvided()
    {
        // A real service identifier is accepted (existence is checked in the handler).
        var validator = new CompleteAppointmentCommandValidator();
        var command = new CompleteAppointmentCommand(Guid.NewGuid(), null, Guid.NewGuid());

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
