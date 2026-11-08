using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Imports;

using Microsoft.EntityFrameworkCore;

namespace DentalClinic.Application.Features.Imports.Patients;

/// <summary>
/// Shared parse + dedupe step for preview and commit, so the dry-run and the
/// real run can never disagree about which rows are acceptable.
/// </summary>
public sealed class PatientImportProcessor(IAppDbContext context, PatientImportParser parser)
{
    public async Task<Result<PatientImportParseResult>> ParseAsync(
        string fileName,
        long length,
        Stream content,
        string? mappingJson,
        CancellationToken cancellationToken)
    {
        if (length <= 0)
        {
            return ImportErrors.EmptyFile;
        }

        if (length > PatientImportRules.MaxFileSizeBytes)
        {
            return ImportErrors.FileTooLarge;
        }

        var parsed = parser.Parse(content, mappingJson);

        if (parsed.IsError)
        {
            return parsed.Errors;
        }

        return await ApplyPhoneRulesAsync(parsed.Value, cancellationToken);
    }

    private async Task<PatientImportParseResult> ApplyPhoneRulesAsync(
        PatientImportParseResult result,
        CancellationToken cancellationToken)
    {
        var validPatients = result.Rows
            .Where(row => row.IsValid)
            .Select(row => row.Patient!)
            .ToList();

        if (validPatients.Count == 0)
        {
            return result;
        }

        var phones = validPatients
            .Select(patient => patient.ContactInfo.PrimaryPhone)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // A primary phone must not collide with another patient's primary or
        // secondary number; the domain has no unique index on phones.
        var existing = await context.Patients
            .AsNoTracking()
            .Where(patient =>
                phones.Contains(patient.ContactInfo.PrimaryPhone) ||
                (patient.ContactInfo.SecondaryPhone != null &&
                 phones.Contains(patient.ContactInfo.SecondaryPhone)))
            .Select(patient => new
            {
                patient.ContactInfo.PrimaryPhone,
                patient.ContactInfo.SecondaryPhone,
            })
            .ToListAsync(cancellationToken);

        var taken = new HashSet<string>(StringComparer.Ordinal);

        foreach (var patient in existing)
        {
            taken.Add(patient.PrimaryPhone);

            if (patient.SecondaryPhone is not null)
            {
                taken.Add(patient.SecondaryPhone);
            }
        }

        var firstRowByPhone = new Dictionary<string, int>(StringComparer.Ordinal);
        var rows = new List<PatientImportRow>(result.Rows.Count);

        foreach (var row in result.Rows)
        {
            if (row.Patient is null)
            {
                rows.Add(row);
                continue;
            }

            var phone = row.Patient.ContactInfo.PrimaryPhone;
            var errors = row.Errors.ToList();

            if (firstRowByPhone.TryGetValue(phone, out var firstRow))
            {
                errors.Add(new PatientImportRowError(
                    row.RowNumber,
                    PatientImportColumns.PrimaryPhone,
                    $"Duplicate phone number in this workbook (already used on row {firstRow}).",
                    phone));
            }
            else
            {
                firstRowByPhone[phone] = row.RowNumber;
            }

            if (taken.Contains(phone))
            {
                errors.Add(new PatientImportRowError(
                    row.RowNumber,
                    PatientImportColumns.PrimaryPhone,
                    "A patient with this phone number already exists.",
                    phone));
            }

            rows.Add(errors.Count == 0 ? row : row with { Errors = errors });
        }

        return result with { Rows = rows };
    }
}