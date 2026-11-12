using Asp.Versioning;

using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Auditing.Dtos;
using DentalClinic.Application.Features.Auditing.Queries.GetAuditLogs;
using DentalClinic.Application.Features.Auditing.Queries.GetAuditLogsForEntity;
using DentalClinic.Contracts.Requests.Auditing;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

/// <summary>
/// Read-only access to the audit trail. Only administrators may see who changed
/// a sensitive record: the trail names staff accounts and exposes before/after
/// values, so it is not staff-visible by default.
/// </summary>
[Route("api/v{version:apiVersion}/audit-logs")]
[ApiVersion("1.0")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves the clinic-wide audit log.")]
    [EndpointDescription("Returns every audited change newest first, optionally filtered by aggregate, record, action, actor and a clinic-local date range. Every filter is optional; page size is capped at 100.")]
    [EndpointName("GetAuditLogs")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] GetAuditLogsRequest request,
        CancellationToken ct)
    {
        var query = new GetAuditLogsQuery(
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            EntityName: request.EntityName,
            EntityId: request.EntityId,
            Action: request.Action,
            UserId: request.UserId,
            From: request.From,
            To: request.To);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{entityName}/{entityId:guid}")]
    [ProducesResponseType(typeof(List<AuditLogEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves the audit history of one record.")]
    [EndpointDescription("Returns every create, update and delete recorded for the supplied record, newest first. The entity name is case-insensitive and accepts the plural form (patient, patients, Patient).")]
    [EndpointName("GetAuditLogsForEntity")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetAuditLogsForEntity(
        string entityName,
        Guid entityId,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetAuditLogsForEntityQuery(entityName, entityId), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}
