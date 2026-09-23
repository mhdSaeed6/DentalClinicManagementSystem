using DentalClinic.Application.Features.Appointments.Commands.CancelAppointment;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.CancelAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CancelAppointmentCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task CancelAppointment_ShouldFailValidation_WhenAppointmentIdIsEmpty()
    {
        // Arrange
        var command = new CancelAppointmentCommand(Guid.Empty);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}