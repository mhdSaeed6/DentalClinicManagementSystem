using DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Commands.AddInvoicePayment;

[Collection(WebAppFactoryCollection.CollectionName)]
public class AddInvoicePaymentCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task AddInvoicePayment_ShouldFailValidation_WhenInvoiceIdIsEmpty()
    {
        // Arrange
        var command = new AddInvoicePaymentCommand(Guid.Empty, 100, "Notes");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AddInvoicePayment_ShouldFailValidation_WhenAmountIsZeroOrNegative(decimal amount)
    {
        // Arrange
        var command = new AddInvoicePaymentCommand(Guid.NewGuid(), amount, "Notes");

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task AddInvoicePayment_ShouldFailValidation_WhenNotesExceed1000Chars()
    {
        // Arrange
        var longNotes = new string('N', 1001);
        var command = new AddInvoicePaymentCommand(Guid.NewGuid(), 50, longNotes);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}