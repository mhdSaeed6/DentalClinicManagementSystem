namespace DentalClinic.Contracts.Requests.LabCases;

public sealed class ChangeLabCaseStatusRequest
{
    /// <summary>
    /// Target status as an integer (<c>Sent=0, AtLab=1, Trial=2, TriedIn=3,
    /// Returned=4, Failed=5</c>). The command validator owns enum validation.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// When the transition happened. Defaults to now when omitted. For
    /// <c>Returned</c> this is the return date; for the <c>Failed → Sent</c>
    /// remake path it becomes the new send date.
    /// </summary>
    public DateTimeOffset? OccurredAtUtc { get; set; }
}