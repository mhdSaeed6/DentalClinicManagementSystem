using DentalClinic.Application.Features.DentalServices.Queries.GetDentalServices;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Queries.GetDentalServices;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDentalServicesQueryValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDentalServices_ShouldFailValidation_WhenPageNumberIsInvalid(int pageNumber)
    {
        // Arrange
        var query = new GetDentalServicesQuery(PageNumber: pageNumber, PageSize: 10);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task GetDentalServices_ShouldFailValidation_WhenPageSizeIsInvalid(int pageSize)
    {
        // Arrange
        var query = new GetDentalServicesQuery(PageNumber: 1, PageSize: pageSize);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}