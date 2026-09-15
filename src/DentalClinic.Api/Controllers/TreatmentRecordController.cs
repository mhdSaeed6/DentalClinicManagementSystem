using Asp.Versioning;

using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.TreatmentRecords.Commands.CreateTreatmentRecord;
using DentalClinic.Application.Features.TreatmentRecords.Commands.UpdateTreatmentRecord;
using DentalClinic.Application.Features.TreatmentRecords.Dtos;
using DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecordById;
using DentalClinic.Application.Features.TreatmentRecords.Queries.GetTreatmentRecords;
using DentalClinic.Contracts.Requests.TreatmentRecords;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/treatment-records")]
[ApiVersion("1.0")]
[Authorize]
public class TreatmentRecordController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TreatmentRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a paginated list of treatment records.")]
    [EndpointDescription("Retrieves treatment records with optional filtering by PatientId or DoctorId.")]
    [EndpointName("GetTreatmentRecords")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetTreatmentRecords([FromQuery] GetTreatmentRecordsRequest request, CancellationToken ct)
    {
        var query = new GetTreatmentRecordsQuery(
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            PatientId: request.PatientId,
            DoctorId: request.DoctorId,
            AppointmentId: request.AppointmentId);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TreatmentRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a treatment record by ID.")]
    [EndpointDescription("Retrieves a single treatment record using its unique Guid.")]
    [EndpointName("GetTreatmentRecordById")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetTreatmentRecordById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTreatmentRecordByIdQuery(id), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TreatmentRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a new treatment record.")]
    [EndpointDescription("Creates a treatment record for a patient with FDI tooth validation.")]
    [EndpointName("CreateTreatmentRecord")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreateTreatmentRecord(
        [FromBody] CreateTreatmentRecordRequest request,
        CancellationToken ct)
    {
        var command = new CreateTreatmentRecordCommand(
            request.PatientId,
            request.DoctorId,
            request.AppointmentId,
            request.ToothNumber,
            request.ProcedureDetails,
            request.Cost);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetTreatmentRecordById), new { id = response.Id }, response),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TreatmentRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Updates an existing treatment record.")]
    [EndpointDescription("Updates a treatment record for a patient with FDI tooth validation.")]
    [EndpointName("UpdateTreatmentRecord")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdateTreatmentRecord(
        Guid id,
        [FromBody] UpdateTreatmentRecordRequest request,
        CancellationToken ct)
    {
        var command = new UpdateTreatmentRecordCommand(
            id,
            request.PatientId,
            request.DoctorId,
            request.AppointmentId,
            request.ToothNumber,
            request.ProcedureDetails,
            request.Cost);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}