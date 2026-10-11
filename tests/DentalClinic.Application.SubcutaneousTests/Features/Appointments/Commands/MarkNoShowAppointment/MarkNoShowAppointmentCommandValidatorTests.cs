using DentalClinic.Application.Features.Appointments.Commands.MarkNoShowAppointment;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.MarkNoShowAppointment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class MarkNoShowAppointmentCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task MarkNoShowAppointment_ShouldFailValidation_WhenAppointmentIdIsEmpty()
    {
        // Arrange
        var command = new MarkNoShowAppointmentCommand(Guid.Empty);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}
