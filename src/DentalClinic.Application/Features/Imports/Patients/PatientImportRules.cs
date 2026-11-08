namespace DentalClinic.Application.Features.Imports.Patients;

public static class PatientImportRules
{
    /// <summary>Matches the request-size limit declared on the controller.</summary>
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>Rows returned by the dry-run preview; the whole file is still validated.</summary>
    public const int PreviewRowLimit = 20;

    /// <summary>Upper bound on errors embedded in a JSON report (the CSV has no cap).</summary>
    public const int MaxReportErrors = 500;
}