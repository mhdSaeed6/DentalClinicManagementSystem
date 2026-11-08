using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Imports;

/// <summary>
/// Audit record of one patient workbook run. A job is only ever Completed when
/// every row was inserted; otherwise it is Failed and carries the rejection
/// diagnostics, which is what makes preview + error reporting mandatory rather
/// than advisory. <see cref="ErrorReportPath"/> is deliberately unused: the CSV
/// report is generated from <see cref="Errors"/> on demand.
/// </summary>
public class ImportJob : AuditableEntity
{
    public string FileName { get; private set; } = default!;
    public ImportJobStatus Status { get; private set; }
    public int TotalRows { get; private set; }
    public int ImportedRows { get; private set; }
    public int RejectedRows { get; private set; }
    public string? ErrorReportPath { get; private set; }
    public string ImportedBy { get; private set; } = default!;

    public List<ImportRowError> Errors { get; private set; } = [];

    private ImportJob()
    { }

    private ImportJob(
        Guid id,
        string fileName,
        int totalRows,
        int importedRows,
        int rejectedRows,
        string importedBy)
        : base(id)
    {
        FileName = fileName;
        Status = rejectedRows > 0 ? ImportJobStatus.Failed : ImportJobStatus.Completed;
        TotalRows = totalRows;
        ImportedRows = importedRows;
        RejectedRows = rejectedRows;
        ImportedBy = importedBy;
    }

    public static Result<ImportJob> Create(
        string fileName,
        string importedBy,
        int totalRows,
        int importedRows,
        int rejectedRows)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ImportErrors.FileRequired;
        }

        if (string.IsNullOrWhiteSpace(importedBy))
        {
            return ImportErrors.ImportedByRequired;
        }

        if (totalRows < 0 || importedRows < 0 || rejectedRows < 0)
        {
            return ImportErrors.InvalidRowCounts;
        }

        if (importedRows > totalRows || rejectedRows > totalRows)
        {
            return ImportErrors.InvalidRowCounts;
        }

        // Completed means every row was inserted; Failed means none were, but
        // the report may describe fewer rows than the file (e.g. two invalid
        // rows in a 50-row file).
        if (rejectedRows == 0 && importedRows != totalRows)
        {
            return ImportErrors.InvalidRowCounts;
        }

        if (rejectedRows > 0 && importedRows != 0)
        {
            return ImportErrors.InvalidRowCounts;
        }

        var safeFileName = Path.GetFileName(fileName.Trim());

        if (safeFileName.Length > 260)
        {
            safeFileName = safeFileName[..260];
        }

        return new ImportJob(
            Guid.NewGuid(),
            safeFileName,
            totalRows,
            importedRows,
            rejectedRows,
            importedBy.Trim());
    }

    public Result<Updated> AddErrors(IEnumerable<ImportRowError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        foreach (var error in errors)
        {
            if (error.ImportJobId != Id)
            {
                return Error.Validation(
                    "ImportJob.ErrorJobMismatch",
                    "A row error belongs to a different import job.");
            }

            Errors.Add(error);
        }

        return Result.Updated;
    }
}