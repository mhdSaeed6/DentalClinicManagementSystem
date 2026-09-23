using DentalClinic.Application.Features.DentalServices.Commands.CreateDentalService;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.DentalServices.Commands.CreateDentalService;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateDentalServiceCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDentalService_ShouldFailValidation_WhenNameIsEmpty(string name)
    {
        var command = CreateValidCommand() with { Name = name };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDentalService_ShouldFailValidation_WhenNameExceeds100Chars()
    {
        var command = CreateValidCommand() with { Name = new string('A', 101) };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDentalService_ShouldFailValidation_WhenDescriptionExceeds500Chars()
    {
        var command = CreateValidCommand() with { Description = new string('B', 501) };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateDentalService_ShouldFailValidation_WhenPriceIsNegative()
    {
        var command = CreateValidCommand() with { Price = -10 };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    private static CreateDentalServiceCommand CreateValidCommand() => new(
        Name: "Teeth Whitening",
        Description: "Cosmetic procedure",
        Price: 150);
}