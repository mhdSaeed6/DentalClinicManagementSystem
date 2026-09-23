using DentalClinic.Application.Features.Invoices.Queries.GetInvoiceById;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Invoices.Queries.GetInvoiceById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetInvoiceByIdValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task GetInvoiceById_ShouldFailValidation_WhenInvoiceIdIsEmpty()
    {
        // Arrange
        var query = new GetInvoiceByIdQuery(Guid.Empty);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}