using Asp.Versioning;

using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Invoices.Commands.AddInvoicePayment;
using DentalClinic.Application.Features.Invoices.Commands.CreateInvoice;
using DentalClinic.Application.Features.Invoices.Dtos;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoiceById;
using DentalClinic.Application.Features.Invoices.Queries.GetInvoices;
using DentalClinic.Contracts.Requests.Invoices;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/invoices")]
[ApiVersion("1.0")]
[Authorize]
public class InvoiceController(ISender sender) : ApiController
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
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetInvoices([FromQuery] GetInvoicesQuery query, CancellationToken ct)
    {
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
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(id), ct);

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