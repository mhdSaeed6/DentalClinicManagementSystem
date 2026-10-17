using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;
using DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointmentWithDraft;

/// <summary>
/// Completes the appointment and then, optionally, generates its invoice draft.
/// Completion always runs first and is persisted on its own, so a failure in the
/// invoicing step can never lose a completed visit or its treatment records.
/// Draft generation itself reuses the existing generator, so pricing rules are
/// never duplicated.
/// </summary>
public class CompleteAppointmentWithDraftCommandHandler(
    ILogger<CompleteAppointmentWithDraftCommandHandler> logger,
    IMediator mediator)
    : IRequestHandler<CompleteAppointmentWithDraftCommand, Result<CompleteAppointmentWithDraftResult>>
{
    public async Task<Result<CompleteAppointmentWithDraftResult>> Handle(
        CompleteAppointmentWithDraftCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Complete the visit through the existing, unchanged workflow.
        var completed = await mediator.Send(
            new CompleteAppointmentCommand(request.AppointmentId, request.Treatments, request.ServiceId),
            cancellationToken);

        if (completed.IsError)
        {
            logger.LogWarning(
                "Complete-with-draft failed for appointment {AppointmentId}. Errors: {@Errors}",
                request.AppointmentId,
                completed.Errors);
            return completed.Errors;
        }

        // 2. Generate the draft from the now-persisted appointment and treatments.
        var generated = await mediator.Send(
            new GenerateAppointmentInvoiceCommand(request.AppointmentId),
            cancellationToken);

        if (generated.IsError)
        {
            // The appointment is already completed and persisted. Report that
            // clearly instead of pretending the whole operation failed.
            var code = generated.TopError.Code;
            logger.LogWarning(
                "Appointment {AppointmentId} completed but no invoice draft was created ({Code}).",
                request.AppointmentId,
                code);

            return new CompleteAppointmentWithDraftResult(
                request.AppointmentId,
                null,
                DraftCreated: false,
                AlreadyHadInvoice: false,
                code);
        }

        var result = generated.Value;

        return new CompleteAppointmentWithDraftResult(
            request.AppointmentId,
            result.InvoiceId,
            DraftCreated: !result.ReusedExisting,
            AlreadyHadInvoice: result.ReusedExisting,
            Reason: null);
    }
}