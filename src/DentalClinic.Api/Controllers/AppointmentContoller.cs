using Asp.Versioning;

using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Appointments.Commands.CancelAppointment;
using DentalClinic.Application.Features.Appointments.Commands.CompleteAppointment;
using DentalClinic.Application.Features.Appointments.Commands.CreateAppointment;
using DentalClinic.Application.Features.Appointments.Commands.RescheduleAppointment;
using DentalClinic.Application.Features.Appointments.Dtos;
using DentalClinic.Application.Features.Appointments.Queries.GetAppointmentById;
using DentalClinic.Application.Features.Appointments.Queries.GetAppointments;
using DentalClinic.Contracts.Requests.Appointments;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/appointments")]
[ApiVersion("1.0")]
// [Authorize]
public class AppointmentContoller(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a paginated list of appointments.")]
    [EndpointDescription("This endpoint retrieves a paginated list of appointments with optional filtering.")]
    [EndpointName("GetAppointments")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetAppointments([FromQuery] GetAppointmentsRequest request, CancellationToken ct)
    {
        var query = new GetAppointmentsQuery(
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            PatientId: request.PatientId,
            DoctorId: request.DoctorId,
            ServiceId: request.ServiceId);
        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves an appointment by ID.")]
    [EndpointDescription("This endpoint retrieves an appointment by its unique ID.")]
    [EndpointName("GetAppointmentById")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetAppointmentById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetAppointmentByIdQuery(id), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a new appointment.")]
    [EndpointDescription("This endpoint creates a new appointment in the system.")]
    [EndpointName("CreateAppointment")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreateAppointment(
        [FromBody] CreateAppointmentRequest request,
        CancellationToken ct)
    {
        var command = new CreateAppointmentCommand(
            request.PatientId,
            request.DoctorId,
            request.ServiceId,
            request.ScheduledDateTime,
            request.DurationInMinutes,
            request.Notes);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetAppointmentById), new { id = response.Id }, response),
            Problem);
    }

    [HttpPut("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Reschedules an existing appointment.")]
    [EndpointDescription("This endpoint reschedules an appointment to a new date and duration.")]
    [EndpointName("RescheduleAppointment")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> RescheduleAppointment(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        CancellationToken ct)
    {
        var command = new RescheduleAppointmentCommand(
            id,
            request.NewScheduledDateTime,
            request.DurationInMinutes);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPatch("{id:guid}/complete")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Marks an appointment as completed.")]
    [EndpointDescription("This endpoint updates the appointment status to completed.")]
    [EndpointName("CompleteAppointment")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CompleteAppointment(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CompleteAppointmentCommand(id), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPatch("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Cancels an appointment.")]
    [EndpointDescription("This endpoint cancels a scheduled appointment.")]
    [EndpointName("CancelAppointment")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CancelAppointmentCommand(id), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }
}