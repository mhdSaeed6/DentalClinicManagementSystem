using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Queries.GetPatientOdontogram;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Odontograms.Queries.GetPatientOdontogram;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetPatientOdontogramValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task GetPatientOdontogram_ShouldFailValidation_WhenPatientIdIsEmpty()
    {
        // Arrange
        var query = new GetPatientOdontogramQuery(Guid.Empty, PatientAccessScope.None);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}