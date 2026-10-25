using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Commands.RemoveToothCondition;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Odontograms;

using MediatR;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Odontograms.Commands.RemoveToothCondition;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveToothConditionCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task Send_WithEmptyPatientId_ShouldFailValidation()
    {
        var command = new RemoveToothConditionCommand(Guid.Empty, 26, PatientAccessScope.None);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Send_WithInvalidToothNumber_ShouldFailValidation()
    {
        // 19 is outside every valid FDI quadrant (11-18, 21-28, ...).
        var command = new RemoveToothConditionCommand(Guid.NewGuid(), 19, PatientAccessScope.None);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }
}