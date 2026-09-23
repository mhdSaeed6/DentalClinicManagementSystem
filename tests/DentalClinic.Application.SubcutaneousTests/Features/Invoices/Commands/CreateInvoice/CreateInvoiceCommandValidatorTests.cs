using DentalClinic.Application.Features.Invoices.Commands.CreateInvoice;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Commands.CreateInvoice;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateInvoiceCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task CreateInvoice_ShouldFailValidation_WhenPatientIdIsEmpty()
    {
        // Arrange
        var command = new CreateInvoiceCommand(Guid.Empty, 200, null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task CreateInvoice_ShouldFailValidation_WhenTotalAmountIsZeroOrNegative(decimal totalAmount)
    {
        // Arrange
        var command = new CreateInvoiceCommand(Guid.NewGuid(), totalAmount, null);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}