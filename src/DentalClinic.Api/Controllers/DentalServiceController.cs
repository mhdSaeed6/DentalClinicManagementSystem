using Asp.Versioning;

using DentalClinic.Application.Features.DentalServices.Commands.CreateDentalService;
using DentalClinic.Application.Features.DentalServices.Commands.RemoveDentalService;
using DentalClinic.Application.Features.DentalServices.Commands.UpdateDentalService;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Application.Features.DentalServices.Queries.GetDentalServiceById;
using DentalClinic.Application.Features.DentalServices.Queries.GetDentalServices;
using DentalClinic.Application.Features.Doctors.Commands.CreateDoctor;
using DentalClinic.Application.Features.Doctors.Commands.RemoveDoctor;
using DentalClinic.Application.Features.Doctors.Commands.UpdateDoctor;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Queries.GetDoctorById;
using DentalClinic.Application.Features.Doctors.Queries.GetDoctors;
using DentalClinic.Contracts.Requests.DentalServices;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/dentalservice")]
[ApiVersion("1.0")]
[Authorize]
public class DentalServiceController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<DentalServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a list of Dental services.")]
    [EndpointDescription("This endpoint retrieves a list of Dental services from the system.")]
    [EndpointName("GetDentalServices")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> Get([FromQuery] GetDentalServicesRequest request, CancellationToken ct)
    {
        var query = new GetDentalServicesQuery(
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            SearchTerm: request.SearchTerm);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DentalServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a Dental service by ID.")]
    [EndpointDescription("This endpoint retrieves a Dental service by their unique ID from the system.")]
    [EndpointName("GetDentalServiceById")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetDentalServiceById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetDentalServiceByIdQuery(id), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DentalServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a new Dental service.")]
    [EndpointDescription("This endpoint creates a new Dental service in the system.")]
    [EndpointName("CreateDentalService")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreateDentalService(
        [FromBody] CreateDentalServiceRequest request,
        CancellationToken ct)
    {
        var command = new CreateDentalServiceCommand(
            request.Name,
            request.Description,
            request.Price);

        var result = await sender.Send(command, ct);

        return result.Match(
        response => CreatedAtAction(nameof(GetDentalServiceById), new { id = response.Id }, response),
        Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Deletes a new Dental service.")]
    [EndpointDescription("This endpoint deletes a new Dental service from the system.")]
    [EndpointName("DeleteDentalService")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> DeleteDentalService(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new RemoveDentalServiceCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DentalServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Updates a new Dental service.")]
    [EndpointDescription("This endpoint updates a new Dental service in the system.")]
    [EndpointName("UpdateDentalService")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdateDentalService(
        Guid id,
        [FromBody] UpdateDentalServiceRequest request,
        CancellationToken ct)
    {
        var command = new UpdateDentalServiceCommand(
            id,
            request.Name,
            request.Description,
            request.Price);

        var result = await sender.Send(command, ct);

        return result.Match(
        response => Ok(response),
        Problem);
    }
}
