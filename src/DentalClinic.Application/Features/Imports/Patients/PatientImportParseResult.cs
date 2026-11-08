using DentalClinic.Domain.Patients;

namespace DentalClinic.Application.Features.Imports.Patients;

/// <summary>One parsed worksheet row plus every reason it cannot be imported.</summary>
public sealed record PatientImportRow(
    int RowNumber,
    Patient? Patient,
    IReadOnlyList<PatientImportRowError> Errors)
{
    public bool IsValid => Patient is not null && Errors.Count == 0;

    public string DisplayName => Patient is null
        ? string.Empty
        : $"{Patient.FirstName} {Patient.LastName}";

    public string PrimaryPhone => Patient?.ContactInfo.PrimaryPhone ?? string.Empty;
}

public sealed record PatientImportRowError(
    int RowNumber,
    string Column,
    string Message,
    string? Value);

/// <summary>
/// Parsed workbook: the resolved mapping, every non-blank data row and the raw
/// header labels, which the Web preview uses to offer an explicit re-mapping.
/// </summary>
public sealed record PatientImportParseResult(
    IReadOnlyList<PatientImportColumnMapping> Columns,
    IReadOnlyList<string> AvailableHeaders,
    IReadOnlyList<PatientImportRow> Rows)
{
    public int TotalRows => Rows.Count;

    public int ValidRows => Rows.Count(row => row.IsValid);

    public int InvalidRows => TotalRows - ValidRows;
}