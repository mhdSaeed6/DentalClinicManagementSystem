namespace DentalClinic.Contracts.Requests.Reports;

/// <summary>
/// The report window, expressed as clinic-local calendar dates. Both ends are
/// inclusive; omit both for the clinic's default rolling window.
/// </summary>
public class GetDashboardSummaryRequest
{
    /// <summary>First clinic-local day of the window (inclusive).</summary>
    public DateOnly? From { get; set; }

    /// <summary>Last clinic-local day of the window (inclusive).</summary>
    public DateOnly? To { get; set; }
}