using DentalClinic.Domain.Imports;

namespace DentalClinic.Application.Features.Imports.Dtos;

/// <summary>One target column and the workbook header it was resolved to.</summary>
public sealed record PatientImportColumnDto(
    string Key,
    string Label,
    bool IsRequired,
    string? SourceHeader);

/// <summary>A single rejected cell, in the shape used by the preview and the report.</summary>
public sealed record ImportRowErrorDto(
    int RowNumber,
    string Column,
    string Message,
    string? Value);

public sealed record PatientImportPreviewRowDto(
    int RowNumber,
    bool IsValid,
    string DisplayName,
    string PrimaryPhone,
    string DateOfBirth,
    string Gender,
    IReadOnlyList<ImportRowErrorDto> Errors);

public sealed record PatientImportPreviewDto(
    IReadOnlyList<PatientImportColumnDto> Columns,
    IReadOnlyList<string> AvailableHeaders,
    IReadOnlyList<PatientImportPreviewRowDto> Rows,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    int PreviewRowLimit);

public sealed record PatientImportReportDto(
    Guid JobId,
    ImportJobStatus Status,
    string FileName,
    int TotalRows,
    int ImportedRows,
    int RejectedRows,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ImportRowErrorDto> Errors);

public sealed record ImportJobDto(
    Guid Id,
    string FileName,
    ImportJobStatus Status,
    int TotalRows,
    int ImportedRows,
    int RejectedRows,
    string ImportedBy,
    DateTimeOffset CreatedAtUtc);

/// <summary>Generated rejection report, streamed as a CSV attachment.</summary>
public sealed record ImportErrorReport(byte[] Content, string FileName);

/// <summary>Generated workbook template, streamed as an .xlsx attachment.</summary>
public sealed record ImportTemplateFile(byte[] Content, string FileName, string ContentType);