using System.Text;

using DentalClinic.Domain.Imports;

namespace DentalClinic.Application.Features.Imports;

/// <summary>
/// Renders the persisted row diagnostics as CSV on demand instead of storing a
/// report file, so the audit trail never lives in the web root.
/// </summary>
public static class ImportErrorCsv
{
    public static byte[] Build(ImportJob job, IEnumerable<ImportRowError> errors)
    {
        var builder = new StringBuilder();
        builder.Append("Row,Column,Message,Value\r\n");

        foreach (var error in errors.OrderBy(error => error.RowNumber).ThenBy(error => error.Column))
        {
            builder
                .Append(error.RowNumber)
                .Append(',')
                .Append(Escape(error.Column))
                .Append(',')
                .Append(Escape(error.Message))
                .Append(',')
                .Append(Escape(error.Value))
                .Append("\r\n");
        }

        // A UTF-8 BOM keeps Excel from mangling Arabic values on open.
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString());
    }

    public static string FileNameFor(ImportJob job) => $"import-{job.Id:N}-errors.csv";

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',', StringComparison.Ordinal) &&
            !value.Contains('"', StringComparison.Ordinal) &&
            !value.Contains('\r', StringComparison.Ordinal) &&
            !value.Contains('\n', StringComparison.Ordinal))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}