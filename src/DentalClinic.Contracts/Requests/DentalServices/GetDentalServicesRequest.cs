namespace DentalClinic.Contracts.Requests.DentalServices;

public class GetDentalServicesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
}