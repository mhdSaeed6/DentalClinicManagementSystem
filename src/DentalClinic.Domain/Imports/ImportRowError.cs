using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Imports;

/// <summary>
/// One rejected cell of an import run. Errors are stored relationally rather
/// than as a file on disk: the rejection report is rendered to CSV on demand,
/// which keeps the audit trail inside the database and out of the web root.
/// </summary>
public class ImportRowError : Entity
{
    public Guid ImportJobId { get; private set; }

    /// <summary>1-based worksheet row, matching what the operator sees in Excel.</summary>
    public int RowNumber { get; private set; }

    public string Column { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string? Value { get; private set; }

    public ImportJob? ImportJob { get; set; }

    private ImportRowError()
    { }

    private ImportRowError(
        Guid importJobId,
        int rowNumber,
        string column,
        string message,
        string? value)
        : base(Guid.NewGuid())
    {
        ImportJobId = importJobId;
        RowNumber = rowNumber;
        Column = column;
        Message = message;
        Value = value;
    }

    public static Result<ImportRowError> Create(
        Guid importJobId,
        int rowNumber,
        string column,
        string message,
        string? value = null)
    {
        if (importJobId == Guid.Empty)
        {
            return Error.Validation("ImportRowError.JobRequired", "The import job is required.");
        }

        if (rowNumber < 1)
        {
            return Error.Validation("ImportRowError.RowNumberInvalid", "The row number must be 1 or greater.");
        }

        if (string.IsNullOrWhiteSpace(column))
        {
            return Error.Validation("ImportRowError.ColumnRequired", "The column name is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return Error.Validation("ImportRowError.MessageRequired", "The error message is required.");
        }

        var trimmedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (trimmedValue is { Length: > 500 })
        {
            trimmedValue = trimmedValue[..500];
        }

        return new ImportRowError(
            importJobId,
            rowNumber,
            column.Trim(),
            message.Trim(),
            trimmedValue);
    }
}