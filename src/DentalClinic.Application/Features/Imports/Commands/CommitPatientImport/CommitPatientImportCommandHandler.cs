using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Imports.Mappers;
using DentalClinic.Application.Features.Imports.Patients;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Identity;
using DentalClinic.Domain.Imports;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.Imports.Commands.CommitPatientImport;

public class CommitPatientImportCommandHandler(
    ILogger<CommitPatientImportCommandHandler> logger,
    IAppDbContext context,
    PatientImportProcessor processor,
    HybridCache cache,
    IUser user)
    : IRequestHandler<CommitPatientImportCommand, Result<PatientImportReportDto>>
{
    public async Task<Result<PatientImportReportDto>> Handle(
        CommitPatientImportCommand request,
        CancellationToken cancellationToken)
    {
        if (!user.IsInRole(Roles.Admin))
        {
            logger.LogWarning("Patient import denied for user {UserId}.", user.Id);
            return ImportErrors.AdminOnly;
        }

        if (string.IsNullOrWhiteSpace(user.Id))
        {
            return ApplicationErrors.UserIdClaimInvalid;
        }

        var parsed = await processor.ParseAsync(
            request.FileName,
            request.Length,
            request.Content,
            request.MappingJson,
            cancellationToken);

        if (parsed.IsError)
        {
            return parsed.Errors;
        }

        var result = parsed.Value;
        var invalidRows = result.Rows.Where(row => !row.IsValid).ToList();

        // Mandatory dry-run equivalent: any rejected row aborts the whole file
        // and the report explains exactly what to fix.
        if (invalidRows.Count > 0)
        {
            var failed = ImportJob.Create(
                request.FileName,
                user.Id,
                result.TotalRows,
                importedRows: 0,
                rejectedRows: invalidRows.Count);

            if (failed.IsError)
            {
                return failed.Errors;
            }

            return await PersistRejectedAsync(failed.Value, invalidRows, cancellationToken);
        }

        var jobResult = ImportJob.Create(
            request.FileName,
            user.Id,
            result.TotalRows,
            importedRows: result.TotalRows,
            rejectedRows: 0);

        if (jobResult.IsError)
        {
            return jobResult.Errors;
        }

        var job = jobResult.Value;
        var patients = result.Rows.Select(row => row.Patient!).ToList();

        // Patients and the audit row are saved in one SaveChanges call, so the
        // import is atomic: either every patient and the Completed job exist,
        // or nothing does.
        context.Patients.AddRange(patients);
        context.ImportJobs.Add(job);

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("patient", cancellationToken);

        logger.LogInformation(
            "Patient import {JobId} completed: {ImportedRows} patient(s) inserted by {UserId}.",
            job.Id,
            patients.Count,
            user.Id);

        return job.ToReportDto([]);
    }

    private async Task<Result<PatientImportReportDto>> PersistRejectedAsync(
        ImportJob job,
        IReadOnlyList<PatientImportRow> invalidRows,
        CancellationToken cancellationToken)
    {
        var errors = new List<ImportRowError>();

        foreach (var row in invalidRows)
        {
            IReadOnlyList<PatientImportRowError> rowErrors = row.Errors.Count > 0
                ? row.Errors
                : [new PatientImportRowError(row.RowNumber, "Row", "The row could not be imported.", null)];

            foreach (var error in rowErrors)
            {
                var created = ImportRowError.Create(
                    job.Id,
                    error.RowNumber,
                    error.Column,
                    error.Message,
                    error.Value);

                if (created.IsError)
                {
                    return created.Errors;
                }

                errors.Add(created.Value);
            }
        }

        var added = job.AddErrors(errors);

        if (added.IsError)
        {
            return added.Errors;
        }

        context.ImportJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Patient import {JobId} rejected: {RejectedRows} of {TotalRows} row(s) invalid.",
            job.Id,
            job.RejectedRows,
            job.TotalRows);

        return job.ToReportDto(errors, PatientImportRules.MaxReportErrors);
    }
}