using Asp.Versioning;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;
using DentalClinic.Application.Features.Invoices.Commands.CreateInvoice;
using DentalClinic.Application.Features.Invoices.Commands.GenerateAppointmentInvoice;
using DentalClinic.Application.Features.Invoices.Commands.IssueInvoice;
using DentalClinic.Application.Features.Invoices.Commands.UpdateInvoiceDraft;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Queries.GetAppointmentInvoiceBreakdown;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoiceById;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoices;
using DentalClinic.Contracts.Requests.Invoices;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/invoices")]
[ApiVersion("1.0")]
[Authorize]
public class InvoiceController(ISender sender, IUser user) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a paginated list of invoices.")]
    [EndpointDescription("Retrieves invoices with optional filtering by PatientId or Status.")]
    [EndpointName("GetInvoices")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetInvoices([FromQuery] GetInvoicesRequest request, CancellationToken ct)
    {
        var query = new GetInvoicesQuery(
            Scope: user.DoctorScope(),
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            PatientId: request.PatientId,
            AppointmentId: request.AppointmentId);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves an invoice by ID.")]
    [EndpointDescription("Retrieves a single invoice along with its payment history using its unique Guid.")]
    [EndpointName("GetInvoiceById")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(id, user.DoctorScope()), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a new invoice.")]
    [EndpointDescription("Creates a new billing invoice for a specified patient and optional appointment.")]
    [EndpointName("CreateInvoice")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken ct)
    {
        var command = new CreateInvoiceCommand(
            request.PatientId,
            request.TotalAmount,
            request.AppointmentId);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetInvoiceById), new { id = response.Id }, response),
            Problem);
    }

    [HttpPost("from-appointment/{appointmentId:guid}")]
    [ProducesResponseType(typeof(GenerateAppointmentInvoiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Generates the invoice draft for an appointment.")]
    [EndpointDescription("Builds one invoice from the appointment's service charge and its performed treatments, linking it to the patient and visit. Repeated requests return the existing draft instead of creating a duplicate.")]
    [EndpointName("GenerateAppointmentInvoice")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GenerateFromAppointment(Guid appointmentId, CancellationToken ct)
    {
        var result = await sender.Send(new GenerateAppointmentInvoiceCommand(appointmentId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("breakdown/{appointmentId:guid}")]
    [ProducesResponseType(typeof(AppointmentInvoiceBreakdownDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Gets the invoice breakdown for an appointment.")]
    [EndpointDescription("Returns the service charge and treatment charges that would be billed for the appointment, using the same pricing rules as draft generation.")]
    [EndpointName("GetAppointmentInvoiceBreakdown")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetBreakdown(Guid appointmentId, CancellationToken ct)
    {
        var result = await sender.Send(new GetAppointmentInvoiceBreakdownQuery(appointmentId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPut("{id:guid}/draft")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Edits a draft invoice.")]
    [EndpointDescription("Replaces the draft's line items and recalculates the total on the server. Issued invoices cannot be edited.")]
    [EndpointName("UpdateInvoiceDraft")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdateDraft(
        Guid id,
        [FromBody] UpdateInvoiceDraftRequest request,
        CancellationToken ct)
    {
        var command = new UpdateInvoiceDraftCommand(
            id,
            [.. request.Lines.Select(l => new UpdateInvoiceLine(l.LineId, l.Description, l.Amount))]);

        var result = await sender.Send(command, ct);

        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{id:guid}/draft/refresh")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Recalculates a draft invoice from its appointment.")]
    [EndpointDescription("Explicitly refreshes a draft's lines from the appointment and its treatments. Manual adjustments are only discarded when explicitly confirmed.")]
    [EndpointName("RefreshInvoiceDraft")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> RefreshDraft(
        Guid id,
        [FromBody] RefreshInvoiceDraftRequest? request,
        CancellationToken ct)
    {
        var command = new RefreshInvoiceDraftCommand(
            id,
            request?.ConfirmDiscardManualAdjustments ?? false);

        var result = await sender.Send(command, ct);

        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{id:guid}/issue")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Issues an invoice draft.")]
    [EndpointDescription("Transitions a draft invoice to issued state. Editing is blocked after issuance.")]
    [EndpointName("IssueInvoice")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new IssueInvoiceCommand(id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{invoiceId:guid}/payments")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Adds a payment to an existing invoice.")]
    [EndpointDescription("Records a new payment against the specified invoice and recalculates the remaining balance.")]
    [EndpointName("AddInvoicePayment")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> AddPayment(
        Guid invoiceId,
        [FromBody] AddInvoicePaymentRequest request,
        CancellationToken ct)
    {
        var command = new AddInvoicePaymentCommand(
            invoiceId,
            request.Amount,
            request.Notes);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}