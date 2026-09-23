using DentalClinic.Application.Features.DentalServices.Commands.UpdateDentalService;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Commands.UpdateDentalService;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateDentalServiceCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task UpdateDentalService_ShouldFailValidation_WhenDentalServiceIdIsEmpty()
    {
        var command = CreateValidCommand() with { DentalServiceId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateDentalService_ShouldFailValidation_WhenNameIsEmpty(string name)
    {
        var command = CreateValidCommand() with { Name = name };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateDentalService_ShouldFailValidation_WhenNameExceeds100Chars()
    {
        var command = CreateValidCommand() with { Name = new string('A', 101) };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateDentalService_ShouldFailValidation_WhenDescriptionExceeds500Chars()
    {
        var command = CreateValidCommand() with { Description = new string('B', 501) };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateDentalService_ShouldFailValidation_WhenPriceIsNegative()
    {
        var command = CreateValidCommand() with { Price = -1 };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    private static UpdateDentalServiceCommand CreateValidCommand() => new(
        DentalServiceId: Guid.NewGuid(),
        Name: "Teeth Cleaning",
        Description: "Routine procedure",
        Price: 120);
}