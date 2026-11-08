using System.Globalization;
using System.Text.Json;

using ClosedXML.Excel;

using DentalClinic.Domain.Common.Enums;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Common.ValueObjects;
using DentalClinic.Domain.Imports;
using DentalClinic.Domain.Patients;

namespace DentalClinic.Application.Features.Imports.Patients;

/// <summary>
/// Reads a patient workbook into domain objects. It deliberately never inserts
/// anything: every row is built through <see cref="Patient.Create"/> and
/// <see cref="ContactInfo.Create"/>, so the import path applies exactly the
/// same rules as the single-patient form.
/// </summary>
public sealed class PatientImportParser
{
    private static readonly JsonSerializerOptions s_mappingJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] s_dateFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "yyyyMMdd",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "MM/dd/yyyy",
        "M/d/yyyy",
        "dd.MM.yyyy",
        "d.M.yyyy",
    ];

    public Result<PatientImportParseResult> Parse(Stream workbookStream, string? mappingJson = null)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);

        Dictionary<string, string>? overrides;

        try
        {
            overrides = ParseOverrides(mappingJson);
        }
        catch (JsonException)
        {
            return ImportErrors.InvalidMapping;
        }

        XLWorkbook workbook;

        try
        {
            workbook = new XLWorkbook(workbookStream);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return ImportErrors.UnreadableWorkbook;
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet is null)
            {
                return ImportErrors.NoDataRows;
            }

            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            if (lastColumn == 0 || lastRow == 0)
            {
                return ImportErrors.NoDataRows;
            }

            var headers = ReadHeaders(worksheet, lastColumn);

            if (headers.Count == 0)
            {
                return ImportErrors.NoDataRows;
            }

            var mappingResult = ResolveMapping(headers, overrides);

            if (mappingResult.IsError)
            {
                return mappingResult.Errors;
            }

            var mappings = mappingResult.Value;

            if (lastRow < 2)
            {
                return ImportErrors.NoDataRows;
            }

            var rows = new List<PatientImportRow>();

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var values = new Dictionary<string, string?>(StringComparer.Ordinal);

                foreach (var mapping in mappings)
                {
                    if (mapping.ColumnIndex > 0)
                    {
                        values[mapping.Key] = ReadCell(worksheet.Cell(rowNumber, mapping.ColumnIndex));
                    }
                }

                // A fully blank row is padding, not a rejected record.
                if (values.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                rows.Add(BuildRow(rowNumber, values));
            }

            if (rows.Count == 0)
            {
                return ImportErrors.NoDataRows;
            }

            return new PatientImportParseResult(
                mappings,
                headers.Select(header => header.Text).ToList(),
                rows);
        }
    }

    private static Dictionary<string, string>? ParseOverrides(string? mappingJson)
    {
        if (string.IsNullOrWhiteSpace(mappingJson))
        {
            return null;
        }

        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson, s_mappingJsonOptions);

        if (raw is null)
        {
            return null;
        }

        return new Dictionary<string, string>(
            raw.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)),
            StringComparer.OrdinalIgnoreCase);
    }

    private static List<(int Index, string Text)> ReadHeaders(IXLWorksheet worksheet, int lastColumn)
    {
        var headers = new List<(int Index, string Text)>();

        for (var column = 1; column <= lastColumn; column++)
        {
            var text = worksheet.Cell(1, column).GetString().Trim();

            if (!string.IsNullOrWhiteSpace(text))
            {
                headers.Add((column, text));
            }
        }

        return headers;
    }

    private static Result<IReadOnlyList<PatientImportColumnMapping>> ResolveMapping(
        IReadOnlyList<(int Index, string Text)> headers,
        Dictionary<string, string>? overrides)
    {
        var mappings = new List<PatientImportColumnMapping>(PatientImportColumns.All.Count);
        var missing = new List<string>();
        var invalidOverrides = new List<string>();

        foreach (var definition in PatientImportColumns.All)
        {
            string? sourceHeader = null;
            var columnIndex = 0;

            if (overrides is not null &&
                overrides.TryGetValue(definition.Key, out var requestedHeader) &&
                !string.IsNullOrWhiteSpace(requestedHeader))
            {
                var match = headers.FirstOrDefault(header =>
                    string.Equals(
                        PatientImportColumns.Normalize(header.Text),
                        PatientImportColumns.Normalize(requestedHeader),
                        StringComparison.Ordinal));

                if (match.Text is null)
                {
                    invalidOverrides.Add(requestedHeader);
                }
                else
                {
                    sourceHeader = match.Text;
                    columnIndex = match.Index;
                }
            }
            else
            {
                var match = FindHeader(headers, definition);

                if (match.Text is not null)
                {
                    sourceHeader = match.Text;
                    columnIndex = match.Index;
                }
            }

            if (sourceHeader is null && definition.IsRequired)
            {
                missing.Add(definition.Label);
            }

            mappings.Add(new PatientImportColumnMapping(
                definition.Key,
                definition.Label,
                definition.IsRequired,
                sourceHeader,
                columnIndex));
        }

        if (invalidOverrides.Count > 0)
        {
            return ImportErrors.MissingRequiredColumns(
                invalidOverrides.Select(header => $"mapped column '{header}'"));
        }

        if (missing.Count > 0)
        {
            return ImportErrors.MissingRequiredColumns(missing);
        }

        return mappings;
    }

    private static (int Index, string? Text) FindHeader(
        IReadOnlyList<(int Index, string Text)> headers,
        PatientImportColumnDefinition definition)
    {
        var candidates = new HashSet<string>(StringComparer.Ordinal)
        {
            PatientImportColumns.Normalize(definition.Key),
            PatientImportColumns.Normalize(definition.Label),
        };

        foreach (var alias in definition.Aliases)
        {
            candidates.Add(PatientImportColumns.Normalize(alias));
        }

        foreach (var header in headers)
        {
            if (candidates.Contains(PatientImportColumns.Normalize(header.Text)))
            {
                return (header.Index, header.Text);
            }
        }

        return (0, null);
    }

    private static PatientImportRow BuildRow(int rowNumber, IReadOnlyDictionary<string, string?> values)
    {
        var errors = new List<PatientImportRowError>();

        var firstName = Text(values, PatientImportColumns.FirstName);
        var lastName = Text(values, PatientImportColumns.LastName);
        var primaryPhone = Text(values, PatientImportColumns.PrimaryPhone);
        var secondaryPhone = Text(values, PatientImportColumns.SecondaryPhone);
        var socialMedia = Text(values, PatientImportColumns.SocialMediaLink);
        var bloodPressure = Text(values, PatientImportColumns.BloodPressureReading);
        var allergies = Text(values, PatientImportColumns.Allergies);
        var notes = Text(values, PatientImportColumns.MedicalNotes);

        if (string.IsNullOrWhiteSpace(firstName))
        {
            errors.Add(new PatientImportRowError(rowNumber, PatientImportColumns.FirstName, "First name is required.", null));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add(new PatientImportRowError(rowNumber, PatientImportColumns.LastName, "Last name is required.", null));
        }

        if (string.IsNullOrWhiteSpace(primaryPhone))
        {
            errors.Add(new PatientImportRowError(rowNumber, PatientImportColumns.PrimaryPhone, "Primary phone is required.", null));
        }

        DateTime? dateOfBirth = null;

        if (Text(values, PatientImportColumns.DateOfBirth) is { } dateText)
        {
            if (TryParseDate(dateText, out var parsedDate))
            {
                dateOfBirth = parsedDate;
            }
            else
            {
                errors.Add(new PatientImportRowError(
                    rowNumber,
                    PatientImportColumns.DateOfBirth,
                    "Date of birth must be a valid date (for example 1990-12-31).",
                    dateText));
            }
        }
        else
        {
            errors.Add(new PatientImportRowError(rowNumber, PatientImportColumns.DateOfBirth, "Date of birth is required.", null));
        }

        var genderText = Text(values, PatientImportColumns.Gender);
        var gender = TryParseGender(genderText);

        if (gender is null)
        {
            errors.Add(new PatientImportRowError(
                rowNumber,
                PatientImportColumns.Gender,
                "Gender must be Male or Female.",
                genderText));
        }

        var whatsApp = TryParseBoolean(Text(values, PatientImportColumns.HasWhatsAppOnPrimary), defaultValue: true, out var whatsAppValid);

        if (!whatsAppValid)
        {
            errors.Add(new PatientImportRowError(
                rowNumber,
                PatientImportColumns.HasWhatsAppOnPrimary,
                "WhatsApp must be Yes or No.",
                Text(values, PatientImportColumns.HasWhatsAppOnPrimary)));
        }

        var hasDiabetes = ParseFlag(rowNumber, values, PatientImportColumns.HasDiabetes, errors);
        var hasHypertension = ParseFlag(rowNumber, values, PatientImportColumns.HasHypertension, errors);
        var hasHeartDisease = ParseFlag(rowNumber, values, PatientImportColumns.HasHeartDisease, errors);
        var hasBleedingDisorder = ParseFlag(rowNumber, values, PatientImportColumns.HasBleedingDisorder, errors);
        var hasEpilepsy = ParseFlag(rowNumber, values, PatientImportColumns.HasEpilepsy, errors);
        var hasAsthma = ParseFlag(rowNumber, values, PatientImportColumns.HasAsthma, errors);
        var isPregnant = ParseFlag(rowNumber, values, PatientImportColumns.IsPregnant, errors);

        int? pregnancyWeeks = null;
        var weeksText = Text(values, PatientImportColumns.PregnancyWeeks);

        if (weeksText is not null)
        {
            if (int.TryParse(weeksText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var weeks) &&
                weeks is >= 0 and <= 45)
            {
                if (isPregnant)
                {
                    pregnancyWeeks = weeks;
                }
            }
            else
            {
                errors.Add(new PatientImportRowError(
                    rowNumber,
                    PatientImportColumns.PregnancyWeeks,
                    "Pregnancy weeks must be a whole number between 0 and 45.",
                    weeksText));
            }
        }

        if (errors.Count > 0)
        {
            // Domain factories are only reached for structurally complete rows;
            // the row still reports every structural error at once.
            return new PatientImportRow(rowNumber, null, errors);
        }

        var contactResult = ContactInfo.Create(
            primaryPhone!,
            secondaryPhone,
            whatsApp,
            socialMedia);

        if (contactResult.IsError)
        {
            var primaryCheck = ContactInfo.Create(primaryPhone!);
            var column = primaryCheck.IsError
                ? PatientImportColumns.PrimaryPhone
                : PatientImportColumns.SecondaryPhone;

            errors.Add(new PatientImportRowError(
                rowNumber,
                column,
                "The phone number is invalid. Use 7-15 digits, optionally prefixed with +.",
                column == PatientImportColumns.PrimaryPhone ? primaryPhone : secondaryPhone));

            return new PatientImportRow(rowNumber, null, errors);
        }

        var medicalHistory = new MedicalHistory
        {
            HasDiabetes = hasDiabetes,
            HasHypertension = hasHypertension,
            HasHeartDisease = hasHeartDisease,
            HasBleedingDisorder = hasBleedingDisorder,
            HasEpilepsy = hasEpilepsy,
            HasAsthma = hasAsthma,
            IsPregnant = isPregnant,
            PregnancyWeeks = pregnancyWeeks,
            BloodPressureReading = bloodPressure,
            Allergies = allergies,
            Notes = notes,
        };

        var patientResult = Patient.Create(
            firstName!,
            lastName!,
            contactResult.Value,
            DateTime.SpecifyKind(dateOfBirth!.Value, DateTimeKind.Utc),
            gender!.Value,
            medicalHistory);

        if (patientResult.IsError)
        {
            errors.Add(new PatientImportRowError(
                rowNumber,
                MapColumn(patientResult.TopError.Code),
                patientResult.TopError.Description,
                null));

            return new PatientImportRow(rowNumber, null, errors);
        }

        return new PatientImportRow(rowNumber, patientResult.Value, []);
    }

    private static bool ParseFlag(
        int rowNumber,
        IReadOnlyDictionary<string, string?> values,
        string column,
        List<PatientImportRowError> errors)
    {
        var text = Text(values, column);

        if (text is null)
        {
            return false;
        }

        var value = TryParseBoolean(text, defaultValue: false, out var valid);

        if (!valid)
        {
            errors.Add(new PatientImportRowError(
                rowNumber,
                column,
                "Medical flags must be Yes or No.",
                text));
        }

        return value;
    }

    private static string? Text(IReadOnlyDictionary<string, string?> values, string key) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;

    private static string? ReadCell(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        return cell.DataType switch
        {
            XLDataType.DateTime => cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            XLDataType.Number => FormatNumber(cell.GetDouble()),
            XLDataType.Boolean => cell.GetBoolean() ? "true" : "false",
            _ => cell.GetString().Trim(),
        };
    }

    private static string FormatNumber(double value) =>
        value == Math.Floor(value)
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.############", CultureInfo.InvariantCulture);

    private static bool TryParseDate(string value, out DateTime date)
    {
        if (DateTime.TryParseExact(
                value.Trim(),
                s_dateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            date = date.Date;
            return true;
        }

        if (DateTime.TryParse(
                value.Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            date = date.Date;
            return true;
        }

        return false;
    }

    private static Gender? TryParseGender(string? value) =>
        PatientImportColumns.Normalize(value ?? string.Empty) switch
        {
            "male" or "m" or "ذكر" or "رجل" => Gender.Male,
            "female" or "f" or "أنثى" or "انثى" or "امرأة" or "امراه" => Gender.Female,
            _ => null,
        };

    private static bool TryParseBoolean(string? value, bool defaultValue, out bool valid)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            valid = true;
            return defaultValue;
        }

        switch (PatientImportColumns.Normalize(value))
        {
            case "true" or "yes" or "y" or "1" or "نعم" or "صح":
                valid = true;
                return true;

            case "false" or "no" or "n" or "0" or "لا" or "خطأ":
                valid = true;
                return false;

            default:
                valid = false;
                return defaultValue;
        }
    }

    private static string MapColumn(string errorCode) => errorCode switch
    {
        "Patient_FirstName_Required" => PatientImportColumns.FirstName,
        "Patient_LastName_Required" => PatientImportColumns.LastName,
        "Patient_DateOfBirth_Invalid" => PatientImportColumns.DateOfBirth,
        "Patient_Gender_Required" => PatientImportColumns.Gender,
        "Common.InvalidPhoneNumber" => PatientImportColumns.PrimaryPhone,
        _ => "Row",
    };
}