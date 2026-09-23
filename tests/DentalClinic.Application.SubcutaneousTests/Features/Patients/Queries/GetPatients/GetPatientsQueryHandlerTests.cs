using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Patients.Queries.GetPatients;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Patients.Queries.GetPatients;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetPatientsQueryHandlerTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();
    private readonly IAppDbContext _context = factory.CreateAppDbContext();

    [Fact]
    public async Task Handle_WithoutSearchTerm_ShouldReturnPaginatedList()
    {
        // Arrange
        await SeedPatientsAsync();
        var query = new GetPatientsQuery(PageNumber: 1, PageSize: 10, SearchTerm: null);

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.TotalCount >= 2);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnFilteredPatients()
    {
        // Arrange
        await SeedPatientsAsync();
        var query = new GetPatientsQuery(PageNumber: 1, PageSize: 10, SearchTerm: "saeed");

        // Act
        var result = await _mediator.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Items!, p => p.FirstName == "Saeed");
    }

    private async Task SeedPatientsAsync()
    {
        var contact1 = ContactInfo.Create("+963911111111", null, true, null).Value;
        var contact2 = ContactInfo.Create("+963922222222", null, true, null).Value;

        var p1 = Patient.Create("Ahmad", "Kareem", contact1, DateTime.UtcNow.AddYears(-25), Gender.Male).Value;
        var p2 = Patient.Create("Saeed", "Mhd", contact2, DateTime.UtcNow.AddYears(-20), Gender.Male).Value;

        await _context.Patients.AddRangeAsync(p1, p2);
        await _context.SaveChangesAsync(default);
    }
}

