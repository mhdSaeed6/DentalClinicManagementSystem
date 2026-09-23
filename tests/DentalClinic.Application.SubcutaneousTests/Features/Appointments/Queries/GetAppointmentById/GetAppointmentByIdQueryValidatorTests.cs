using DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Queries.GetAppointmentById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetAppointmentByIdQueryValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task GetAppointmentById_ShouldFailValidation_WhenAppointmentIdIsEmpty()
    {
        // Arrange
        var query = new GetAppointmentByIdQuery(Guid.Empty);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}