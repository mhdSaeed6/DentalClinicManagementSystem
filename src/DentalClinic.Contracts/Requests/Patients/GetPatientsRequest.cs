namespace DentalClinic.Contracts.Requests.Patients;

public class GetPatientsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
}