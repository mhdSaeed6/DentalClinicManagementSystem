using Asp.Versioning;

using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Security;
using DentalClinic.Application.Features.PatientImages.Commands.DeletePatientImage;
using DentalClinic.Application.Features.PatientImages.Commands.UploadPatientImage;
using DentalClinic.Application.Features.PatientImages.Dtos;
using DentalClinic.Application.Features.PatientImages.Queries.GetPatientImageContent;
using DentalClinic.Application.Features.PatientImages.Queries.GetPatientImages;
using DentalClinic.Domain.Patients;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/patients/{patientId:guid}/images")]
[ApiVersion("1.0")]
[Authorize(Policy = "StaffOnly")]
public sealed class PatientImageController(ISender sender, IUser user) : ApiController
{
    private const long MaxRequestSizeBytes = 12 * 1024 * 1024;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PatientImageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Lists a patient's clinical images.")]
    [EndpointDescription("Returns X-ray and before/after image metadata for one patient.")]
    [EndpointName("GetPatientImages")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetPatientImages(Guid patientId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPatientImagesQuery(patientId, user.PatientScope()), ct);

        return result.Match(
            response => Ok(response),
            Problem);
    }

    [HttpGet("{imageId:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Streams a stored clinical image.")]
    [EndpointDescription("Serves the binary inline; the Web app fetches it with the bearer token instead of using a raw <img> URL.")]
    [EndpointName("GetPatientImageContent")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetPatientImageContent(Guid patientId, Guid imageId, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetPatientImageContentQuery(patientId, imageId, user.PatientScope()),
            ct);

        return result.Match(
            content => File(content.Content, content.ContentType),
            Problem);
    }

    [HttpPost]
    [Authorize(Policy = "ClinicalWrite")]
    [RequestSizeLimit(MaxRequestSizeBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PatientImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Uploads a clinical image.")]
    [EndpointDescription("Accepts JPEG, PNG or WebP up to 10 MB. The format is verified from the file's magic bytes, not the declared header.")]
    [EndpointName("UploadPatientImage")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> UploadPatientImage(
        Guid patientId,
        [FromForm] IFormFile? file,
        [FromForm] Guid? treatmentRecordId,
        [FromForm] int? toothNumber,
        [FromForm] int? kind,
        [FromForm] DateTimeOffset? capturedAtUtc,
        CancellationToken ct)
    {
        if (file is null)
        {
            return Problem([PatientImageErrors.FileRequired]);
        }

        if (kind is null)
        {
            return Problem([PatientImageErrors.KindRequired]);
        }

        await using var content = file.OpenReadStream();

        var command = new UploadPatientImageCommand(
            patientId,
            treatmentRecordId,
            toothNumber,
            (PatientImageKind)kind.Value,
            file.FileName,
            file.ContentType,
            file.Length,
            content,
            capturedAtUtc);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtAction(
                nameof(GetPatientImages),
                new { patientId },
                response),
            Problem);
    }

    [HttpDelete("{imageId:guid}")]
    [Authorize(Policy = "ClinicalWrite")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Deletes a clinical image.")]
    [EndpointDescription("Soft-deletes the image row and removes the stored binary.")]
    [EndpointName("DeletePatientImage")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> DeletePatientImage(Guid patientId, Guid imageId, CancellationToken ct)
    {
        var result = await sender.Send(new DeletePatientImageCommand(patientId, imageId), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}