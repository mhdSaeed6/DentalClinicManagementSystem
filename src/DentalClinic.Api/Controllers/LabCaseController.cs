using Asp.Versioning;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.LabCases.Commands.ChangeLabCaseStatus;
using DentalClinic.Application.Features.LabCases.Commands.CreateLabCase;
using DentalClinic.Application.Features.LabCases.Commands.DeleteLabCase;
using DentalClinic.Application.Features.LabCases.Commands.UpdateLabCase;
using DentalClinic.Application.Features.LabCases.Dtos;
using DentalClinic.Application.Features.LabCases.Queries.GetLabCaseById;
using DentalClinic.Application.Features.LabCases.Queries.GetLabCases;
using DentalClinic.Contracts.Requests.LabCases;
using DentalClinic.Domain.Labs;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/lab-cases")]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffOnly")]
public sealed class LabCaseController(ISender sender, IUser user) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<LabCaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves dental lab cases.")]
    [EndpointDescription("Retrieves lab cases with optional filtering by patient, doctor, status, and overdue state.")]
    [EndpointName("GetLabCases")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetLabCases([FromQuery] GetLabCasesRequest request, CancellationToken ct)
    {
        // GetLabCasesQueryValidator owns status validation (IsInEnum); do not
        // duplicate it here.
        var status = request.Status.HasValue ? (LabCaseStatus)request.Status.Value : (LabCaseStatus?)null;

        var query = new GetLabCasesQuery(
            Scope: user.DoctorScope(),
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            PatientId: request.PatientId,
            DoctorId: request.DoctorId,
            Status: status,
            Overdue: request.Overdue);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LabCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a lab case by ID.")]
    [EndpointDescription("Retrieves a single dental lab case using its unique Guid.")]
    [EndpointName("GetLabCaseById")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetLabCaseById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetLabCaseByIdQuery(id, user.DoctorScope()), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(LabCaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a lab case.")]
    [EndpointDescription("Opens a dental lab case for a patient, optionally linked to a treatment record.")]
    [EndpointName("CreateLabCase")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreateLabCase(
        [FromBody] CreateLabCaseRequest request,
        CancellationToken ct)
    {
        var command = new CreateLabCaseCommand(
            request.PatientId,
            request.DoctorId,
            request.TreatmentRecordId,
            request.ToothNumber,
            request.LabName,
            request.CaseType,
            request.SentAtUtc,
            request.ExpectedReturnAtUtc,
            request.Cost,
            request.Notes);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetLabCaseById), new { id = response.Id }, response),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(LabCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Updates a lab case.")]
    [EndpointDescription("Updates the operational data of an open lab case. Returned and failed cases are closed to edits.")]
    [EndpointName("UpdateLabCase")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdateLabCase(
        Guid id,
        [FromBody] UpdateLabCaseRequest request,
        CancellationToken ct)
    {
        var command = new UpdateLabCaseCommand(
            id,
            request.TreatmentRecordId,
            request.ToothNumber,
            request.LabName,
            request.CaseType,
            request.ExpectedReturnAtUtc,
            request.Cost,
            request.Notes);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(LabCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Advances a lab case through the workflow.")]
    [EndpointDescription("Applies one legal status transition; the domain transition map rejects anything else with 409.")]
    [EndpointName("ChangeLabCaseStatus")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> ChangeLabCaseStatus(
        Guid id,
        [FromBody] ChangeLabCaseStatusRequest request,
        CancellationToken ct)
    {
        var command = new ChangeLabCaseStatusCommand(
            id,
            (LabCaseStatus)request.Status,
            request.OccurredAtUtc ?? DateTimeOffset.UtcNow);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Deletes a lab case.")]
    [EndpointDescription("Soft-deletes a dental lab case by its unique Guid.")]
    [EndpointName("DeleteLabCase")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> DeleteLabCase(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteLabCaseCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}