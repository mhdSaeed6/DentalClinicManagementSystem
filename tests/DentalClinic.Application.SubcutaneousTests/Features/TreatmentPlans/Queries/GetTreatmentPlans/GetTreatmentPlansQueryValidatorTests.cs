using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlans;
using DentalClinic.Application.SubcutaneousTests.Common;

using MediatR;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentPlans.Queries.GetTreatmentPlans;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetTreatmentPlansQueryValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTreatmentPlans_ShouldFailValidation_WhenPageNumberIsInvalid(int pageNumber)
    {
        var query = new GetTreatmentPlansQuery(Scope: DoctorAccessScope.None, PageNumber: pageNumber, PageSize: 10);

        var result = await _mediator.Send(query);

        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetTreatmentPlans_ShouldFailValidation_WhenPageSizeIsInvalid(int pageSize)
    {
        var query = new GetTreatmentPlansQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: pageSize);

        var result = await _mediator.Send(query);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task GetTreatmentPlans_ShouldPassValidation_WithValidPaging()
    {
        var query = new GetTreatmentPlansQuery(Scope: DoctorAccessScope.None, PageNumber: 1, PageSize: 25);

        var result = await _mediator.Send(query);

        Assert.True(result.IsSuccess);
    }
}