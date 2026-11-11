namespace DentalClinic.Domain.Auditing;

/// <summary>
/// Canonical <see cref="AuditLogEntry.EntityName"/> values.
///
/// The EF interceptor writes the CLR type name, and the query normalizes
/// whatever the caller supplied onto the same value so <c>patient</c>,
/// <c>patients</c> and <c>Patient</c> all resolve to one history.
/// </summary>
public static class AuditEntityNames
{
    public const string Patient = "Patient";
    public const string Appointment = "Appointment";
    public const string Invoice = "Invoice";
    public const string TreatmentRecord = "TreatmentRecord";

    public static IReadOnlyList<string> All { get; } =
    [
        Patient,
        Appointment,
        Invoice,
        TreatmentRecord,
    ];

    /// <summary>
    /// Maps a caller-supplied name onto its canonical form. Matching is
    /// case-insensitive and tolerates the plural form used by REST routes.
    /// </summary>
    public static bool TryNormalize(string? entityName, out string canonical)
    {
        canonical = string.Empty;

        if (string.IsNullOrWhiteSpace(entityName))
        {
            return false;
        }

        var candidate = entityName.Trim();

        foreach (var known in All)
        {
            if (string.Equals(known, candidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(known + "s", candidate, StringComparison.OrdinalIgnoreCase))
            {
                canonical = known;
                return true;
            }
        }

        return false;
    }
}
