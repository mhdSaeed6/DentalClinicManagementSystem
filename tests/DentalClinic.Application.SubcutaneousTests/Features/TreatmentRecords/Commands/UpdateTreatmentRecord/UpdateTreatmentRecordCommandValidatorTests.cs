using DentalClinic.Application.Features.TreatmentRecords.Commands.UpdateTreatmentRecord;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Commands.UpdateTreatmentRecord;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateTreatmentRecordCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task UpdateTreatmentRecord_ShouldFailValidation_WhenTreatmentRecordIdIsEmpty()
    {
        var command = CreateValidCommand() with { TreatmentRecordId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateTreatmentRecord_ShouldFailValidation_WhenPatientIdIsEmpty()
    {
        var command = CreateValidCommand() with { PatientId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateTreatmentRecord_ShouldFailValidation_WhenDoctorIdIsEmpty()
    {
        var command = CreateValidCommand() with { DoctorId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    public async Task UpdateTreatmentRecord_ShouldFailValidation_WhenToothNumberIsInvalid(int toothNumber)
    {
        var command = CreateValidCommand() with { ToothNumber = toothNumber };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateTreatmentRecord_ShouldFailValidation_WhenProcedureDetailsIsEmpty(string procedureDetails)
    {
        var command = CreateValidCommand() with { ProcedureDetails = procedureDetails };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task UpdateTreatmentRecord_ShouldFailValidation_WhenCostIsNegative()
    {
        var command = CreateValidCommand() with { Cost = -5 };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    private static UpdateTreatmentRecordCommand CreateValidCommand() => new(
        TreatmentRecordId: Guid.NewGuid(),
        PatientId: Guid.NewGuid(),
        DoctorId: Guid.NewGuid(),
        AppointmentId: null,
        ToothNumber: 21,
        ProcedureDetails: "Updated Crown Placement",
        Cost: 300);
}