using Asp.Versioning;

using DentalClinic.Application.Features.Doctors.Commands.CreateDoctor;
using DentalClinic.Application.Features.Doctors.Commands.RemoveDoctor;
using DentalClinic.Application.Features.Doctors.Commands.UpdateDoctor;
using DentalClinic.Application.Features.Doctors.Dtos;
using DentalClinic.Application.Features.Doctors.Queries.GetDoctorById;
using DentalClinic.Application.Features.Doctors.Queries.GetDoctors;
using DentalClinic.Contracts.Requests.Doctors;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/doctors")]
[ApiVersion("1.0")]
[Authorize]
public class DoctorController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a list of doctors.")]
    [EndpointDescription("This endpoint retrieves a list of doctors from the system.")]
    [EndpointName("GetDoctors")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetDoctors([FromQuery] GetPatientsRequest request, CancellationToken ct)
    {
        var query = new GetDoctorsQuery(
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            SearchTerm: request.SearchTerm);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a doctor by ID.")]
    [EndpointDescription("This endpoint retrieves a doctor by their unique ID from the system.")]
    [EndpointName("GetDoctorById")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetDoctorById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetDoctorByIdQuery(id), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a new doctor.")]
    [EndpointDescription("This endpoint creates a new doctor in the system.")]
    [EndpointName("CreateDoctor")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreateDoctor(
        [FromBody] CreateDoctorRequest request,
        CancellationToken ct)
    {
        var genderEnum = Enum.Parse<Gender>(request.Gender, ignoreCase: true);

        var contactInfo = ContactInfo.Create(
                request.ContactInfo.PrimaryPhone,
                request.ContactInfo.SecondaryPhone,
                request.ContactInfo.HasWhatsAppOnPrimary,
                request.ContactInfo.SocialMediaLink).Value;

        var command = new CreateDoctorCommand(
            request.FirstName,
            request.LastName,
            request.Specialization,
            contactInfo,
            genderEnum);

        var result = await sender.Send(command, ct);

        return result.Match(
        response => CreatedAtAction(nameof(GetDoctorById), new { id = response.Id }, response),
        Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Deletes a new doctor.")]
    [EndpointDescription("This endpoint deletes a new doctor from the system.")]
    [EndpointName("DeleteDoctor")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> DeleteDoctor(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        var result = await sender.Send(new RemoveDoctorCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Updates a new doctor.")]
    [EndpointDescription("This endpoint updates a new doctor in the system.")]
    [EndpointName("UpdateDoctor")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdateDoctor(
        Guid id,
        [FromBody] UpdateDoctorRequest request,
        CancellationToken ct)
    {
        var genderEnum = Enum.Parse<Gender>(request.Gender, ignoreCase: true);

        var contactInfo = ContactInfo.Create(
            request.ContactInfo.PrimaryPhone,
            request.ContactInfo.SecondaryPhone,
            request.ContactInfo.HasWhatsAppOnPrimary,
            request.ContactInfo.SocialMediaLink).Value;

        var command = new UpdateDoctorCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Specialization,
            contactInfo,
            genderEnum);

        var result = await sender.Send(command, ct);

        return result.Match(
        response => Ok(response),
        Problem);
    }
}
