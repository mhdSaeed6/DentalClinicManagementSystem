using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Commands.RemoveDentalService;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Commands.RemoveDentalService;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveDentalServiceCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithExistingService_ShouldSoftDeleteAndSaveToDb()
    {
        // Arrange
        var service = await SeedServiceAsync();
        var command = new RemoveDentalServiceCommand(service.Id);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var dbService = await _context.DentalServices
        .AsNoTracking()
        .IgnoreQueryFilters() // لفحص حالة الـ Soft Delete
            .FirstOrDefaultAsync(s => s.Id == service.Id);

        Assert.NotNull(dbService);
        Assert.True(dbService.IsDeleted);
    }

    [Fact]
    public async Task Handle_WithNonExistingServiceId_ShouldReturnServiceNotFound()
    {
        // Arrange
        var command = new RemoveDentalServiceCommand(Guid.NewGuid());

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    private async Task<DentalService> SeedServiceAsync()
    {
        var service = DentalService.Create("Dental Scaling", "description", 45).Value;
        await _context.DentalServices.AddAsync(service);
        await _context.SaveChangesAsync(default);

        return service;
    }
}