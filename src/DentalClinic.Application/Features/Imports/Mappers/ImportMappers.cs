using System.Globalization;

using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Imports.Patients;
using DentalClinic.Domain.Imports;

namespace DentalClinic.Application.Features.Imports.Mappers;

public static class ImportMappers
{
    public static PatientImportPreviewDto ToPreviewDto(
        this PatientImportParseResult result,
        int previewRowLimit)
    {
        var rows = result.Rows
            .Take(previewRowLimit)
            .Select(row => new PatientImportPreviewRowDto(
                row.RowNumber,
                row.IsValid,
                row.DisplayName,
                row.PrimaryPhone,
                row.Patient?.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
                row.Patient?.Gender.ToString() ?? string.Empty,
                row.Errors.ToDtos()))
            .ToList();

        return new PatientImportPreviewDto(
            result.Columns
                .Select(column => new PatientImportColumnDto(
                    column.Key,
                    column.Label,
                    column.IsRequired,
                    column.SourceHeader))
                .ToList(),
            result.AvailableHeaders,
            rows,
            result.TotalRows,
            result.ValidRows,
            result.InvalidRows,
            previewRowLimit);
    }

    public static PatientImportReportDto ToReportDto(
        this ImportJob job,
        IEnumerable<ImportRowError> errors,
        int maxErrors = int.MaxValue) =>
        new(
            job.Id,
            job.Status,
            job.FileName,
            job.TotalRows,
            job.ImportedRows,
            job.RejectedRows,
            job.CreatedAtUtc,
            errors
                .Take(maxErrors)
                .Select(error => new ImportRowErrorDto(
                    error.RowNumber,
                    error.Column,
                    error.Message,
                    error.Value))
                .ToList());

    public static ImportJobDto ToDto(this ImportJob job) =>
        new(
            job.Id,
            job.FileName,
            job.Status,
            job.TotalRows,
            job.ImportedRows,
            job.RejectedRows,
            job.ImportedBy,
            job.CreatedAtUtc);

    public static IReadOnlyList<ImportRowErrorDto> ToDtos(this IEnumerable<PatientImportRowError> errors) =>
        errors
            .Select(error => new ImportRowErrorDto(
                error.RowNumber,
                error.Column,
                error.Message,
                error.Value))
            .ToList();
}