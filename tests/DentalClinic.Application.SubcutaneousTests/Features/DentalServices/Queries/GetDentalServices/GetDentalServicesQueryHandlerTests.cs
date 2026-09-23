using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Queries.GetDentalServices;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Services;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Queries.GetDentalServices;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDentalServicesQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithSearchTermAndPagination_ShouldReturnFilteredPaginatedList()
    {
        // Arrange
        var service1 = DentalService.Create("Laser koko", "description2", 60).Value;
        var service2 = DentalService.Create("Teeth Cleaning", "description", 30).Value;

        await _context.DentalServices.AddRangeAsync(service1, service2);
        await _context.SaveChangesAsync(default);

        var query = new GetDentalServicesQuery(PageNumber: 1, PageSize: 10, SearchTerm: "koko");

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Single(result.Value.Items!);
        Assert.Equal("Laser koko", result.Value.Items!.First().Name);
    }

    [Fact]
    public async Task Handle_WhenNoMatchingRecords_ShouldReturnEmptyPaginatedList()
    {
        // Arrange
        var query = new GetDentalServicesQuery(PageNumber: 1, PageSize: 10, SearchTerm: "NonExistingServiceKeyword");

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items!);
        Assert.Equal(0, result.Value.TotalCount);
    }
}