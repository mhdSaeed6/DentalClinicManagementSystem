using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Commands.UpdateDentalService;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Commands.UpdateDentalService;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateDentalServiceCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateServiceAndSaveToDb()
    {
        // Arrange
        var service = await SeedServiceAsync();
        var command = new UpdateDentalServiceCommand(service.Id, "Updated Name", "Updated Description", 150);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Name", result.Value.Name);
        Assert.Equal(150, result.Value.Price);

        var dbService = await _context.DentalServices
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == service.Id);

        Assert.NotNull(dbService);
        Assert.Equal("Updated Name", dbService.Name);
        Assert.Equal("Updated Description", dbService.Description);
        Assert.Equal(150, dbService.Price);
    }

    [Fact]
    public async Task Handle_WithNonExistingServiceId_ShouldReturnServiceNotFound()
    {
        // Arrange
        var command = new UpdateDentalServiceCommand(Guid.NewGuid(), "Name", "Description", 100);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<DentalService> SeedServiceAsync()
    {
        var service = DentalService.Create("Old Name", "description", 30).Value;
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return service;
    }
}