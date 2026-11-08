using Asp.Versioning;

using DentalClinic.Application.Features.Imports.Commands.CommitPatientImport;
using DentalClinic.Application.Features.Imports.Commands.PreviewPatientImport;
using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Imports.Queries.GetImportJob;
using DentalClinic.Application.Features.Imports.Queries.GetImportJobErrors;
using DentalClinic.Application.Features.Imports.Queries.GetPatientImportTemplate;
using DentalClinic.Domain.Imports;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.Api.Controllers;

[Route("api/v{version:apiVersion}/imports")]
[ApiVersion("1.0")]
[Authorize(Policy = "AdminOnly")]
public sealed class ImportController(ISender sender) : ApiController
{
    private const long MaxRequestSizeBytes = 10 * 1024 * 1024;

    [HttpPost("patients/preview")]
    [RequestSizeLimit(MaxRequestSizeBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PatientImportPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Dry-runs a patient workbook.")]
    [EndpointDescription("Parses and validates every row through the same domain factories as the real import, without writing anything.")]
    [EndpointName("PreviewPatientImport")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> PreviewPatientImport(
        [FromForm] IFormFile? file,
        [FromForm] string? mapping,
        CancellationToken ct)
    {
        if (file is null)
        {
            return Problem([ImportErrors.FileRequired]);
        }

        await using var content = file.OpenReadStream();

        var result = await sender.Send(
            new PreviewPatientImportCommand(file.FileName, file.Length, content, mapping),
            ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost("patients")]
    [RequestSizeLimit(MaxRequestSizeBytes)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PatientImportReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Imports patients from a workbook.")]
    [EndpointDescription("All-or-nothing: a workbook with any rejected row creates a Failed job and returns its error report; a valid workbook inserts every patient in one transaction.")]
    [EndpointName("CommitPatientImport")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> CommitPatientImport(
        [FromForm] IFormFile? file,
        [FromForm] string? mapping,
        CancellationToken ct)
    {
        if (file is null)
        {
            return Problem([ImportErrors.FileRequired]);
        }

        await using var content = file.OpenReadStream();

        var result = await sender.Send(
            new CommitPatientImportCommand(file.FileName, file.Length, content, mapping),
            ct);

        // A rejected workbook is a successful, fully reported request; only an
        // unreadable file or an invalid mapping is a 400.
        return result.Match(Ok, Problem);
    }

    [HttpGet("patients/template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Downloads the patient-import template.")]
    [EndpointDescription("Returns an .xlsx workbook whose headers already match the importer, plus an instructions sheet.")]
    [EndpointName("GetPatientImportTemplate")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetPatientImportTemplate(CancellationToken ct)
    {
        var result = await sender.Send(new GetPatientImportTemplateQuery(), ct);

        return result.Match(
            template => File(template.Content, template.ContentType, template.FileName),
            Problem);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ImportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Gets one import job.")]
    [EndpointDescription("Returns the persisted audit row for a preview or commit run.")]
    [EndpointName("GetImportJob")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetImportJob(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetImportJobQuery(id), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}/errors")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointSummary("Downloads a rejection report.")]
    [EndpointDescription("Renders the persisted row diagnostics as a CSV attachment.")]
    [EndpointName("GetImportJobErrors")]
    [MapToApiVersion("1.0")]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetImportJobErrors(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetImportJobErrorsQuery(id), ct);

        return result.Match(
            report => File(report.Content, "text/csv", report.FileName),
            Problem);
    }
}