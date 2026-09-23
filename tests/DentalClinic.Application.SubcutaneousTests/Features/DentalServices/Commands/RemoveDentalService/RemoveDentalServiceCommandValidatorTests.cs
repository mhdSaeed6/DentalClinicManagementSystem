using DentalClinic.Application.Features.DentalServices.Commands.RemoveDentalService;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Commands.RemoveDentalService;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveDentalServiceCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task RemoveDentalService_ShouldFailValidation_WhenDentalServiceIdIsEmpty()
    {
        // Arrange
        var command = new RemoveDentalServiceCommand(Guid.Empty);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}