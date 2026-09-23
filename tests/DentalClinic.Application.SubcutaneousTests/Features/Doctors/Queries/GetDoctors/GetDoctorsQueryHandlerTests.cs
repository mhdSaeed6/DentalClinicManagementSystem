using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Doctors.Queries.GetDoctors;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Doctors;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Doctors.Queries.GetDoctors;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetDoctorsQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithoutSearchTerm_ShouldReturnPaginatedList()
    {
        // Arrange
        await SeedDoctorsAsync();
        var query = new GetDoctorsQuery(PageNumber: 1, PageSize: 2, SearchTerm: null);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Items!.Count);
        Assert.True(result.Value.TotalCount >= 3);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(2, result.Value.PageSize);
    }

    [Fact]
    public async Task Handle_WithSearchTermMatchingName_ShouldReturnFilteredDoctors()
    {
        // Arrange
        await SeedDoctorsAsync();
        var query = new GetDoctorsQuery(PageNumber: 1, PageSize: 10, SearchTerm: "saeed");

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Items!, d => d.FirstName == "Saeed");
    }

    [Fact]
    public async Task Handle_WithSearchTermMatchingSpecialization_ShouldReturnFilteredDoctors()
    {
        // Arrange
        await SeedDoctorsAsync();
        var query = new GetDoctorsQuery(PageNumber: 1, PageSize: 10, SearchTerm: "ortho");

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Items!, d => d.Specialization == "Orthodontics");
    }

    [Fact]
    public async Task Handle_WithPaginationSkipAndTake_ShouldReturnCorrectPage()
    {
        // Arrange
        await SeedDoctorsAsync();
        var queryPage1 = new GetDoctorsQuery(PageNumber: 1, PageSize: 1, SearchTerm: null);
        var queryPage2 = new GetDoctorsQuery(PageNumber: 2, PageSize: 1, SearchTerm: null);

        // Act
        var resultPage1 = await _mediator.Send(queryPage1);
        var resultPage2 = await _mediator.Send(queryPage2);

        // Assert
        Assert.True(resultPage1.IsSuccess);
        Assert.True(resultPage2.IsSuccess);
        Assert.Single(resultPage1.Value.Items!);
        Assert.Single(resultPage2.Value.Items!);
        Assert.NotEqual(resultPage1.Value.Items!.First().Id, resultPage2.Value.Items!.First().Id);
    }

    private async Task SeedDoctorsAsync()
    {
        var contact1 = ContactInfo.Create("+963911111111", null, true, "https://fb.com/dr1").Value;
        var contact2 = ContactInfo.Create("+963922222222", "+963933333333", true, null).Value;
        var contact3 = ContactInfo.Create("+963944444444", null, false, null).Value;

        var doc1 = Doctor.Create("Ahmad", "Kareem", "Pediatric Dentistry", contact1, Gender.Male).Value;
        var doc2 = Doctor.Create("Saeed", "Mhd", "Orthodontics", contact2, Gender.Male).Value;
        var doc3 = Doctor.Create("Lina", "Hassan", "Endodontics", contact3, Gender.Female).Value;

        await _context.Doctors.AddRangeAsync(doc1, doc2, doc3);
        await _context.SaveChangesAsync(default);
    }
}