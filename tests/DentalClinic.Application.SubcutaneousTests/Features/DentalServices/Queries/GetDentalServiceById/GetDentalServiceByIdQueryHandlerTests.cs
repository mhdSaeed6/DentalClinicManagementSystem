using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Queries.GetDentalServiceById;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Services;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Queries.GetDentalServiceById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDentalServiceByIdQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingService_ShouldReturnServiceDto()
    {
        // Arrange
        var service = await SeedServiceAsync();
        var query = new GetDentalServiceByIdQuery(service.Id);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(service.Id, result.Value.Id);
        Assert.Equal("Dental Filling", result.Value.Name);
    }

    [Fact]
    public async Task Handle_WithNonExistingServiceId_ShouldReturnServiceNotFound()
    {
        // Arrange
        var query = new GetDentalServiceByIdQuery(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<DentalService> SeedServiceAsync()
    {
        var service = DentalService.Create("Dental Filling", "description", 45).Value;
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return service;
    }
}