using ClosedXML.Excel;

using DentalClinic.Application.Features.Imports.Dtos;
using DentalClinic.Application.Features.Imports.Patients;

namespace DentalClinic.Application.Features.Imports.Patients;

/// <summary>
/// Builds the downloadable workbook so operators start from a sheet whose
/// headers already match the importer. The example row is a formatting guide,
/// not data: the importer skips it only if the operator removes it, so the
/// instructions sheet tells them to.
/// </summary>
public sealed class PatientImportTemplateBuilder
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public ImportTemplateFile Build()
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add("Patients");
        var instructions = workbook.Worksheets.Add("Instructions");

        var columns = PatientImportColumns.All;

        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var cell = sheet.Cell(1, index + 1);
            cell.Value = column.Label;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(column.IsRequired ? "#0F766E" : "#64748B");
        }

        var example = new Dictionary<string, XLCellValue>(StringComparer.Ordinal)
        {
            [PatientImportColumns.FirstName] = "Sara",
            [PatientImportColumns.LastName] = "Ahmed",
            [PatientImportColumns.PrimaryPhone] = "+201234567890",
            [PatientImportColumns.DateOfBirth] = new DateTime(1990, 12, 31),
            [PatientImportColumns.Gender] = "Female",
            [PatientImportColumns.HasWhatsAppOnPrimary] = "Yes",
            [PatientImportColumns.HasDiabetes] = "No",
            [PatientImportColumns.Allergies] = "Penicillin",
        };

        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var cell = sheet.Cell(2, index + 1);

            if (example.TryGetValue(column.Key, out var value))
            {
                cell.Value = value;
            }

            cell.Style.Font.Italic = true;
            cell.Style.Font.FontColor = XLColor.Gray;

            if (column.Key == PatientImportColumns.DateOfBirth)
            {
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
            }
        }

        sheet.Row(1).Height = 22;
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns().AdjustToContents();

        instructions.Cell(1, 1).Value = "Patient import template";
        instructions.Cell(1, 1).Style.Font.Bold = true;
        instructions.Cell(1, 1).Style.Font.FontSize = 16;

        var lines = new List<string>
        {
            string.Empty,
            "How to use this workbook",
            "1. Replace the grey example row with your data, or add rows below it. Delete the example row before importing.",
            "2. Keep the header row in row 1. Column order does not matter; headers are matched by name.",
            "3. Required columns: First name, Last name, Primary phone, Date of birth, Gender.",
            "4. Dates: use yyyy-mm-dd (for example 1990-12-31).",
            "5. Gender: Male or Female.",
            "6. Yes/No columns accept Yes/No, true/false or 1/0.",
            "7. Phone numbers: 7-15 digits, optionally prefixed with +. Duplicates in the file or against an existing patient reject the run.",
            "8. Import is all-or-nothing: fix every rejected row and retry. Nothing is written until the whole file is valid.",
            string.Empty,
            "Optional columns",
        };

        lines.AddRange(columns
            .Where(column => !column.IsRequired)
            .Select(column => $"- {column.Label}"));

        for (var index = 0; index < lines.Count; index++)
        {
            instructions.Cell(index + 2, 1).Value = lines[index];
        }

        instructions.Column(1).Width = 110;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ImportTemplateFile(
            stream.ToArray(),
            "patient-import-template.xlsx",
            ContentType);
    }
}