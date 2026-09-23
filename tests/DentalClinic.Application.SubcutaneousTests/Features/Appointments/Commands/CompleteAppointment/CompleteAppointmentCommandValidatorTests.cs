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
    }
}