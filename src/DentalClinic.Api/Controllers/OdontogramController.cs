using Asp.Versioning;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.Odontograms.Commands.RemoveToothCondition;
using DentalClinic.Application.Features.Odontograms.Commands.UpsertToothCondition;
using DentalClinic.Application.Features.Odontograms.Dtos;
using DentalClinic.Application.Features.Odontograms.Queries.GetPatientOdontogram;
using DentalClinic.Contracts.Requests.Odontograms;
using DentalClinic.Domain.Common.Enums;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/patients/{patientId:guid}/odontogram")]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffOnly")]
public class OdontogramController(ISender sender, IUser user) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PatientOdontogramDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves the odontogram of a patient.")]
    [EndpointDescription("Retrieves the patient's per-tooth condition rows together with every treatment record grouped for the odontogram chart.")]
    [EndpointName("GetPatientOdontogram")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetPatientOdontogram(Guid patientId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPatientOdontogramQuery(patientId, user.PatientScope()), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPut("teeth/{toothNumber:int}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(PatientToothSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Sets the condition of a single tooth.")]
    [EndpointDescription("Upserts the live condition row for one FDI tooth of a patient: an existing row is updated, otherwise a new row is created.")]
    [EndpointName("UpsertToothCondition")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpsertToothCondition(
        Guid patientId,
        int toothNumber,
        [FromBody] UpsertToothConditionRequest request,
        CancellationToken ct)
    {
        var command = new UpsertToothConditionCommand(
            patientId,
            toothNumber,
            (ToothCondition)request.Condition,
            (ToothSurface?)request.Surface,
            request.Notes,
            user.PatientScope());

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("teeth/{toothNumber:int}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Clears the condition of a single tooth.")]
    [EndpointDescription("Soft-deletes the live condition row for one FDI tooth of a patient; clearing a tooth without a live row is an idempotent success.")]
    [EndpointName("RemoveToothCondition")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> RemoveToothCondition(Guid patientId, int toothNumber, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveToothConditionCommand(patientId, toothNumber, user.PatientScope()), ct);

        return result.Match(
            _ => Ok(),
            Problem);
    }
}