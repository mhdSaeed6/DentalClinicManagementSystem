namespace DentalClinic.Domain.Patients;

/// <summary>
/// Clinical image categories. Values are stored as ints and are append-only, so
/// adding a new kind never rewrites existing rows.
/// </summary>
public enum PatientImageKind
{
    Xray = 0,
    Before = 1,
    After = 2,
    Other = 3
}