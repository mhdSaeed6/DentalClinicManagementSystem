using Asp.Versioning;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.TreatmentPlans.Commands.ActivateTreatmentPlan;
using DentalClinic.Application.Features.TreatmentPlans.Commands.AddTreatmentPlanItem;
using DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlan;
using DentalClinic.Application.Features.TreatmentPlans.Commands.CancelTreatmentPlanItem;
using DentalClinic.Application.Features.TreatmentPlans.Commands.CompleteTreatmentPlan;
using DentalClinic.Application.Features.TreatmentPlans.Commands.CreateTreatmentPlan;
using DentalClinic.Application.Features.TreatmentPlans.Commands.PerformTreatmentPlanItem;
using DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlan;
using DentalClinic.Application.Features.TreatmentPlans.Commands.RemoveTreatmentPlanItem;
using DentalClinic.Application.Features.TreatmentPlans.Commands.ReopenTreatmentPlanItem;
using DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlan;
using DentalClinic.Application.Features.TreatmentPlans.Commands.UpdateTreatmentPlanItem;
using DentalClinic.Application.Features.TreatmentPlans.Dtos;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanById;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlans;
using DentalClinic.Application.Features.TreatmentPlans.Queries.GetTreatmentPlanSessions;
using DentalClinic.Contracts.Requests.TreatmentPlans;
using DentalClinic.Domain.TreatmentPlans;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/treatment-plans")]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffOnly")]
public sealed class TreatmentPlanController(ISender sender, IUser user) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<TreatmentPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Retrieves treatment plans.")]
    [EndpointName("GetTreatmentPlans")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetPlans([FromQuery] GetTreatmentPlansRequest request, CancellationToken ct)
    {
        // GetTreatmentPlansQueryValidator owns status validation (IsInEnum);
        // do not duplicate it here.
        var status = request.Status.HasValue ? (TreatmentPlanStatus)request.Status.Value : (TreatmentPlanStatus?)null;

        var result = await sender.Send(
            new GetTreatmentPlansQuery(
            user.DoctorScope(),
            request.PageNumber,
            request.PageSize,
            request.PatientId,
            request.DoctorId,
            status), ct);

        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(TreatmentPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a treatment plan.")]
    [EndpointName("CreateTreatmentPlan")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateTreatmentPlanRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateTreatmentPlanCommand(
            request.PatientId,
            request.DoctorId,
            request.Title,
            request.Notes), ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetPlan), new { id = response.Id }, response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TreatmentPlanDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Retrieves a treatment plan with its items.")]
    [EndpointName("GetTreatmentPlanById")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetPlan(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTreatmentPlanByIdQuery(user.DoctorScope(), id), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates a treatment plan.")]
    [EndpointName("UpdateTreatmentPlan")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateTreatmentPlanRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateTreatmentPlanCommand(id, request.Title, request.Notes), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Soft-deletes a treatment plan.")]
    [EndpointName("RemoveTreatmentPlan")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RemovePlan(Guid id, [FromQuery] RemoveTreatmentPlanRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveTreatmentPlanCommand(id, request.ConfirmPerformedItems), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointName("ActivateTreatmentPlan")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ActivatePlan(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ActivateTreatmentPlanCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointName("CompleteTreatmentPlan")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CompletePlan(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteTreatmentPlanCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointName("CancelTreatmentPlan")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CancelPlan(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CancelTreatmentPlanCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(TreatmentPlanItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointName("AddTreatmentPlanItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddTreatmentPlanItemRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new AddTreatmentPlanItemCommand(
            id,
            request.ToothNumber,
            request.ProcedureDetails,
            request.EstimatedCost,
            request.SessionNumber), ct);

        return result.Match(response => CreatedAtAction(nameof(GetPlan), new { id }, response), Problem);
    }

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(TreatmentPlanItemDto), StatusCodes.Status200OK)]
    [EndpointName("UpdateTreatmentPlanItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        Guid itemId,
        [FromBody] UpdateTreatmentPlanItemRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new UpdateTreatmentPlanItemCommand(
            id,
            itemId,
            request.ToothNumber,
            request.ProcedureDetails,
            request.EstimatedCost,
            request.SessionNumber), ct);

        return result.Match(response => Ok(response), Problem);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointName("RemoveTreatmentPlanItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveTreatmentPlanItemCommand(id, itemId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/perform")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(TreatmentPlanItemDto), StatusCodes.Status200OK)]
    [EndpointName("PerformTreatmentPlanItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> PerformItem(
        Guid id,
        Guid itemId,
        [FromBody] PerformTreatmentPlanItemRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new PerformTreatmentPlanItemCommand(
            id,
            itemId,
            request.ActualCost,
            request.AppointmentId), ct);

        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/cancel")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(TreatmentPlanItemDto), StatusCodes.Status200OK)]
    [EndpointName("CancelTreatmentPlanItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> CancelItem(Guid id, Guid itemId, CancellationToken ct)
    {
        var result = await sender.Send(new CancelTreatmentPlanItemCommand(id, itemId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpPost("{id:guid}/items/{itemId:guid}/reopen")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(typeof(TreatmentPlanItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointName("ReopenTreatmentPlanItem")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> ReopenItem(Guid id, Guid itemId, CancellationToken ct)
    {
        var result = await sender.Send(new ReopenTreatmentPlanItemCommand(id, itemId), ct);
        return result.Match(response => Ok(response), Problem);
    }

    [HttpGet("{id:guid}/sessions")]
    [ProducesResponseType(typeof(List<TreatmentPlanSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointName("GetTreatmentPlanSessions")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetSessions(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTreatmentPlanSessionsQuery(user.DoctorScope(), id), ct);
        return result.Match(response => Ok(response), Problem);
    }
}