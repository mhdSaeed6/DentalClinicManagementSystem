using DentalClinic.Domain.Common.Results;

namespace DentalClinic.Domain.Imports;

public static class ImportErrors
{
    public static readonly Error FileRequired = Error.Validation(
        "Import.FileRequired",
        "An .xlsx workbook is required.");

    public static readonly Error EmptyFile = Error.Validation(
        "Import.EmptyFile",
        "The uploaded workbook is empty.");

    public static readonly Error FileTooLarge = Error.Validation(
        "Import.FileTooLarge",
        "The workbook exceeds the 10 MB import limit.");

    public static readonly Error UnreadableWorkbook = Error.Validation(
        "Import.UnreadableWorkbook",
        "The uploaded file is not a readable .xlsx workbook.");

    public static readonly Error NoDataRows = Error.Validation(
        "Import.NoDataRows",
        "The workbook contains no data rows below the header.");

    public static readonly Error InvalidMapping = Error.Validation(
        "Import.InvalidMapping",
        "The column mapping is not valid.");

    public static readonly Error JobNotFound = Error.NotFound(
        "Import.JobNotFound",
        "Import job not found.");

    public static readonly Error AdminOnly = Error.Forbidden(
        "Import.AdminOnly",
        "Only administrators may import patient data.");

    public static readonly Error ImportedByRequired = Error.Validation(
        "Import.ImportedByRequired",
        "The importing account is required.");

    public static readonly Error InvalidRowCounts = Error.Validation(
        "Import.InvalidRowCounts",
        "Imported and rejected row counts must add up to the total row count.");

    public static Error MissingRequiredColumns(IEnumerable<string> columns) =>
        Error.Validation(
            "Import.MissingRequiredColumns",
            $"The workbook is missing required column(s): {string.Join(", ", columns)}.");
}