using DentalClinic.Application.Features.DentalServices.Queries.GetDentalServiceById;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Queries.GetDentalServiceById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDentalServiceByIdQueryValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task GetDentalServiceById_ShouldFailValidation_WhenDentalServiceIdIsEmpty()
    {
        // Arrange
        var query = new GetDentalServiceByIdQuery(Guid.Empty);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}