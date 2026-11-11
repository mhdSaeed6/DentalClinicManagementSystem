namespace DentalClinic.Contracts.Requests.Auditing;

/// <summary>
/// Filters for the administrator audit log. Every filter is optional; the
/// server applies only the ones that were supplied and always sorts newest
/// first.
/// </summary>
public class GetAuditLogsRequest
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    /// <summary>Aggregate name: <c>Patient</c>, <c>Appointment</c>, <c>Invoice</c>
    /// or <c>TreatmentRecord</c>. Case-insensitive; the plural form is accepted.</summary>
    public string? EntityName { get; set; }

    /// <summary>Restricts the history to one record of the chosen aggregate.</summary>
    public Guid? EntityId { get; set; }

    /// <summary><c>Create</c>, <c>Update</c> or <c>Delete</c>.</summary>
    public string? Action { get; set; }

    /// <summary>Identity id of the staff account that made the change.</summary>
    public string? UserId { get; set; }

    /// <summary>First clinic-local day to include (inclusive).</summary>
    public DateOnly? From { get; set; }

    /// <summary>Last clinic-local day to include (inclusive).</summary>
    public DateOnly? To { get; set; }
}
