namespace DentalClinic.Domain.Auditing;

/// <summary>
/// The kind of change a single audit entry describes.
/// </summary>
public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
}
