using Asp.Versioning;

using DentalClinic.Application.Common.Models;
using DentalClinic.Application.Features.Patients.Commands.CreatePatient;
using DentalClinic.Application.Features.Patients.Commands.RemovePatient;
using DentalClinic.Application.Features.Patients.Commands.UpdatePatient;
using DentalClinic.Application.Features.Patients.Dtos;
using DentalClinic.Application.Features.Patients.Queries.GetPatientById;
using DentalClinic.Application.Features.Patients.Queries.GetPatients;
using DentalClinic.Contracts.Requests.Patients;
using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/patients")]
[ApiVersion("1.0")]
// [Authorize]
public class PatientController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a paginated list of patients.")]
    [EndpointDescription("This endpoint retrieves a paginated list of patients with optional search filtering.")]
    [EndpointName("GetPatients")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetPatients([FromQuery] GetPatientsRequest request, CancellationToken ct)
    {
        var query = new GetPatientsQuery(
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            SearchTerm: request.SearchTerm);

        var result = await sender.Send(query, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Retrieves a patient by ID.")]
    [EndpointDescription("This endpoint retrieves a patient by their unique ID from the system.")]
    [EndpointName("GetPatientById")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    [OutputCache(Duration = 60)]
    public async Task<IActionResult> GetPatientById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPatientByIdQuery(id), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Creates a new patient.")]
    [EndpointDescription("This endpoint creates a new patient in the system.")]
    [EndpointName("CreatePatient")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CreatePatient(
        [FromBody] CreatePatientRequest request,
        CancellationToken ct)
    {
        var genderEnum = Enum.Parse<Gender>(request.Gender, ignoreCase: true);

        var medicalHistory = request.MedicalHistory is not null
            ? new MedicalHistory
            {
                HasDiabetes = request.MedicalHistory.HasDiabetes,
                HasHypertension = request.MedicalHistory.HasHypertension,
                HasHeartDisease = request.MedicalHistory.HasHeartDisease,
                Allergies = request.MedicalHistory.Allergies,
                Notes = request.MedicalHistory.Notes
            }
            : new MedicalHistory();

        var command = new CreatePatientCommand(
            request.FirstName,
            request.LastName,
            ContactInfo.Create(
                request.ContactInfo.PrimaryPhone,
                request.ContactInfo.SecondaryPhone,
                request.ContactInfo.HasWhatsAppOnPrimary,
                request.ContactInfo.SocialMediaLink).Value,
            request.DateOfBirth,
            genderEnum,
            medicalHistory);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtAction(nameof(GetPatientById), new { id = response.Id }, response),
            Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Updates an existing patient.")]
    [EndpointDescription("This endpoint updates patient details by their unique ID.")]
    [EndpointName("UpdatePatient")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UpdatePatient(
        Guid id,
        [FromBody] UpdatePatientRequest request,
        CancellationToken ct)
    {
        var genderEnum = Enum.Parse<Gender>(request.Gender, ignoreCase: true);

        var medicalHistory = request.MedicalHistory is not null
            ? new MedicalHistory
            {
                HasDiabetes = request.MedicalHistory.HasDiabetes,
                HasHypertension = request.MedicalHistory.HasHypertension,
                HasHeartDisease = request.MedicalHistory.HasHeartDisease,
                Allergies = request.MedicalHistory.Allergies,
                Notes = request.MedicalHistory.Notes
            }
            : new MedicalHistory();

        var command = new UpdatePatientCommand(
            id,
            request.FirstName,
            request.LastName,
            ContactInfo.Create(request.ContactInfo.PrimaryPhone,
                               request.ContactInfo.SecondaryPhone,
                               request.ContactInfo.HasWhatsAppOnPrimary,
                               request.ContactInfo.SocialMediaLink).Value,
            request.DateOfBirth,
            genderEnum,
            medicalHistory);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Deletes a patient.")]
    [EndpointDescription("This endpoint removes a patient from the system by their unique ID.")]
    [EndpointName("RemovePatient")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> RemovePatient(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new RemovePatientCommand(id), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}