using DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;
using DentalClinic.Application.SubcutaneousTests.Common;

using MediatR;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Commands.GenerateAppointmentInvoice;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GenerateAppointmentInvoiceCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task GenerateAppointmentInvoice_ShouldFailValidation_WhenAppointmentIdIsEmpty()
    {
        // Arrange
        var command = new GenerateAppointmentInvoiceCommand(Guid.Empty);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        Assert.True(result.IsError);
    }
}
