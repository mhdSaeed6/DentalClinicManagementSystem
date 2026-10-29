using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecords;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Queries.GetTreatmentRecords;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentRecordsQueryValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTreatmentRecords_ShouldFailValidation_WhenPageNumberIsInvalid(int pageNumber)
    {
        // Arrange
        var query = new GetTreatmentRecordsQuery(Scope: DoctorAccessScope.None, PageNumber: pageNumber, PageSize: 10);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task GetTreatmentRecords_ShouldFailValidation_WhenPageSizeIsInvalid(int pageSize)
    {
        // Arrange
        var query = new GetTreatmentRecordsQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: pageSize);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(39)]
    [InlineData(40)]
    [InlineData(49)]
    [InlineData(50)]
    [InlineData(56)]
    [InlineData(60)]
    [InlineData(66)]
    [InlineData(70)]
    [InlineData(76)]
    [InlineData(80)]
    [InlineData(86)]
    public async Task GetTreatmentRecords_ShouldFailValidation_WhenToothNumberIsInvalid(int toothNumber)
    {
        // Arrange
        var query = new GetTreatmentRecordsQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: 10, ToothNumber: toothNumber);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task GetTreatmentRecords_ShouldPassValidation_WhenToothNumberIsValid()
    {
        // Arrange
        var query = new GetTreatmentRecordsQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: 10, ToothNumber: 26);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
    }
}