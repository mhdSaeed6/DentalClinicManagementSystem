using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;
using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointmentWithDraft;
using DentalClinic.Application.SubcutaneousTests.Common;
using MediatR;
using Xunit;

namespace DentalClinic.Application.SubcutaneousTests.Features.Appointments.Commands.CompleteAppointmentWithDraft;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CompleteAppointmentWithDraftCommandValidatorTests(WebAppFactory factory)
{
    private readonly IMediator _mediator = factory.CreateMediator();

    [Fact]
    public async Task CompleteWithDraft_ShouldPassValidation_WhenTreatmentToothNumberIsNull()
    {
        // S8: a treatment may be recorded without a tooth — validation must pass
        // and the failure must come later from the handler (Appointment.NotFound).
        var command = new CompleteAppointmentWithDraftCommand(
            Guid.NewGuid(),
            [new CompleteAppointmentTreatment(null, "General consultation", 10m)]);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
        Assert.NotEqual("ToothNumber", result.TopError.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(19)] // ليس رقماً FDI صالحاً (القاعدة القديمة 1-32 كانت تقبله)
    [InlineData(99)]
    public async Task CompleteWithDraft_ShouldFailValidation_WhenTreatmentToothNumberIsInvalid(int toothNumber)
    {
        // S8: the tooth rule here was aligned with the FDI single source — the
        // old InclusiveBetween(1, 32) accepted non-FDI values like 19.
        var command = new CompleteAppointmentWithDraftCommand(
            Guid.NewGuid(),
            [new CompleteAppointmentTreatment(toothNumber, "Composite filling", 10m)]);

        var result = await _mediator.Send(command);

        Assert.True(result.IsError);
        Assert.Contains("ToothNumber", result.TopError.Code);
    }
}