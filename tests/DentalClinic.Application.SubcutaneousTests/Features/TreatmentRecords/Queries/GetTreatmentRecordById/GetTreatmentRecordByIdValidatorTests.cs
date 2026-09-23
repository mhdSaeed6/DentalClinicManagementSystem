using DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecordById;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Queries.GetTreatmentRecordById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentRecordByIdValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task GetTreatmentRecordById_ShouldFailValidation_WhenTreatmentRecordIdIsEmpty()
    {
        // Arrange
        var query = new GetTreatmentRecordByIdQuery(Guid.Empty);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}