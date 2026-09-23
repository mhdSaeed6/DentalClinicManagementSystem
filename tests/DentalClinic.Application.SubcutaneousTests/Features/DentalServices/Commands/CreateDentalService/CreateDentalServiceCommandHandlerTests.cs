using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Commands.CreateDentalService;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Commands.CreateDentalService;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateDentalServiceCommandHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateServiceAndSaveToDb()
    {
        // Arrange
        var command = new CreateDentalServiceCommand("Root Canal Treatment", "Endodontic procedure", 250);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Root Canal Treatment", result.Value.Name);
        Assert.Equal(250, result.Value.Price);

        var dbService = await _context.DentalServices
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == result.Value.Id);

        Assert.NotNull(dbService);
        Assert.Equal("Root Canal Treatment", dbService.Name);
    }
}