namespace DentalClinic.Contracts.Requests.LabCases;

public sealed class GetLabCasesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public int? Status { get; set; }

    /// <summary>
    /// <c>true</c> returns only open cases whose expected return date has passed;
    /// <c>false</c> returns only the remaining cases; <c>null</c> returns both.
    /// </summary>
    public bool? Overdue { get; set; }
}