using DentalClinic.Application.Features.TreatmentRecords.Commands.CreateTreatmentRecord;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.TreatmentRecords.Commands.CreateTreatmentRecord;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateTreatmentRecordCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task CreateTreatmentRecord_ShouldFailValidation_WhenPatientIdIsEmpty()
    {
        var command = CreateValidCommand() with { PatientId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateTreatmentRecord_ShouldFailValidation_WhenDoctorIdIsEmpty()
    {
        var command = CreateValidCommand() with { DoctorId = Guid.Empty };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)] // رقم غير موجود بنظام FDI
    [InlineData(99)] // خارج نطاق الأسنان الدائمة واللبنية
    [InlineData(-11)]
    public async Task CreateTreatmentRecord_ShouldFailValidation_WhenToothNumberIsInvalid(int toothNumber)
    {
        var command = CreateValidCommand() with { ToothNumber = toothNumber };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTreatmentRecord_ShouldFailValidation_WhenProcedureDetailsIsEmpty(string procedureDetails)
    {
        var command = CreateValidCommand() with { ProcedureDetails = procedureDetails };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    [Fact]
    public async Task CreateTreatmentRecord_ShouldFailValidation_WhenCostIsNegative()
    {
        var command = CreateValidCommand() with { Cost = -20 };
        var result = await _mediator.Send(command);
        Assert.True(result.IsError);
    }

    private static CreateTreatmentRecordCommand CreateValidCommand() => new(
        PatientId: Guid.NewGuid(),
        DoctorId: Guid.NewGuid(),
        AppointmentId: null,
        ToothNumber: 16,
        ProcedureDetails: "Composite Filling",
        Cost: 100);
}
