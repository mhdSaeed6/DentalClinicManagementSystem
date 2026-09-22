using DentalClinic.Application.Features.Doctors.Queries.GetDoctors;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Queries.GetDoctors;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDoctorsQueryValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDoctors_ShouldFailValidation_WhenPageNumberIsZeroOrNegative(int pageNumber)
    {
        // Arrange
        var query = new GetDoctorsQuery(PageNumber: pageNumber, PageSize: 10, SearchTerm: null);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)] // يتجاوز الحد الأقصى 100
    public async Task GetDoctors_ShouldFailValidation_WhenPageSizeIsInvalid(int pageSize)
    {
        // Arrange
        var query = new GetDoctorsQuery(PageNumber: 1, PageSize: pageSize, SearchTerm: null);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task GetDoctors_ShouldFailValidation_WhenSearchTermExceedsMaxLength()
    {
        // Arrange
        var longSearchTerm = new string('a', 101);
        var query = new GetDoctorsQuery(PageNumber: 1, PageSize: 10, SearchTerm: longSearchTerm);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }
}