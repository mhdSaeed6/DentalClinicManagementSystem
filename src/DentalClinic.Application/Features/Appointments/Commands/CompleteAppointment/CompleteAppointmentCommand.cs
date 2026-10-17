using DentalClinic.Domain.Common.Results;

using MediatR;

namespace DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;

/// <summary>
/// A treatment performed during the visit, captured as part of completing the
/// appointment. The patient, doctor and appointment link are always derived
/// server-side from the appointment being completed, so they are intentionally
/// not accepted from the caller.
/// </summary>
public sealed record CompleteAppointmentTreatment(
    int ToothNumber,
    string ProcedureDetails,
    decimal Cost);

/// <summary>
/// Completes an appointment and, optionally, records the treatments performed
/// during that visit and the service that was actually carried out.
/// An empty (or omitted) <see cref="Treatments"/> collection completes a
/// consultation-only visit without creating any treatment record.
/// An omitted <see cref="ServiceId"/> leaves the appointment's current service
/// untouched, so callers that complete without a body keep the booked service.
/// </summary>
public sealed record CompleteAppointmentCommand(
    Guid AppointmentId,
    IReadOnlyList<CompleteAppointmentTreatment>? Treatments = null,
    Guid? ServiceId = null) : IRequest<Result<Updated>>;

/// <summary>
/// Outcome of the "complete and create invoice draft" option. The appointment is
/// completed even when no invoice could be produced, so the UI can tell the user
/// precisely what happened rather than showing a generic failure.
/// </summary>
public sealed record CompleteAppointmentWithDraftResult(
    Guid AppointmentId,
    Guid? InvoiceId,
    bool DraftCreated,
    bool AlreadyHadInvoice,
    string? Reason);

public sealed record CompleteAppointmentWithDraftCommand(
    Guid AppointmentId,
    IReadOnlyList<CompleteAppointmentTreatment>? Treatments = null,
    Guid? ServiceId = null) : IRequest<Result<CompleteAppointmentWithDraftResult>>;
