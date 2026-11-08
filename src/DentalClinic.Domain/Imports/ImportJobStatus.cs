namespace DentalClinic.Domain.Imports;

/// <summary>
/// Outcome of one bulk-import run. A run that rejects any row is persisted as
/// <see cref="Failed"/> and keeps its row diagnostics so the operator can fix
/// the workbook and retry; nothing is ever partially imported.
/// </summary>
public enum ImportJobStatus
{
    Completed = 0,
    Failed = 1,
}