namespace DentalClinic.Domain.Labs;

/// <summary>
/// Lifecycle of a dental-lab case. The workflow: a case is <c>Sent</c> to the
/// external lab, the lab works on it (<c>AtLab</c>), it comes back once for a
/// try-in (<c>Trial</c>) — after which the try-in is done and the case goes
/// back for finishing (<c>TriedIn</c>) — and finally the finished prosthesis is
/// <c>Returned</c>. A case that the lab cannot deliver is <c>Failed</c>; a
/// failed case may be re-sent (remake), which restarts the same lifecycle.
/// </summary>
public enum LabCaseStatus
{
    Sent = 0,
    AtLab = 1,
    Trial = 2,
    TriedIn = 3,
    Returned = 4,
    Failed = 5
}