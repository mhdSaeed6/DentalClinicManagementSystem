using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Commands.UpsertToothCondition;
using DentalClinic.Application.SubcutaneousTests.Common;
using DentalClinic.Domain.Common.Enums;

using MediatR;

using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Odontograms.Commands.UpsertToothCondition;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpsertToothConditionCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task Send_WithEmptyPatientId_ShouldFailValidation()
    {
        var command = new UpsertToothConditionCommand(
            Guid.Empty, 26, ToothCondition.Caries, null, null, PatientAccessScope.None);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Send_WithInvalidToothNumber_ShouldFailValidation()
    {
        // 19 is outside every valid FDI quadrant (11-18, 21-28, ...).
        var command = new UpsertToothConditionCommand(
            Guid.NewGuid(), 19, ToothCondition.Caries, null, null, PatientAccessScope.None);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Send_WithUndefinedCondition_ShouldFailValidation()
    {
        var command = new UpsertToothConditionCommand(
            Guid.NewGuid(), 26, (ToothCondition)99, null, null, PatientAccessScope.None);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Send_WithUndefinedSurface_ShouldFailValidation()
    {
        var command = new UpsertToothConditionCommand(
            Guid.NewGuid(), 26, ToothCondition.Caries, (ToothSurface)9, null, PatientAccessScope.None);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }

    [Fact]
    public async Task Send_WithNotesOverFiveHundredCharacters_ShouldFailValidation()
    {
        var command = new UpsertToothConditionCommand(
            Guid.NewGuid(), 26, ToothCondition.Caries, null, new string('x', 501), PatientAccessScope.None);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
    }
}