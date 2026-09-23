using DentalClinic.Application.Features.Doctors.Queries.GetDoctorById;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Queries.GetDoctorById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDoctorByIdQueryValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task GetDoctorById_ShouldFailValidation_WhenDoctorIdIsEmpty()
    {
        // Arrange
        var query = new GetDoctorByIdQuery(Guid.Empty);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}