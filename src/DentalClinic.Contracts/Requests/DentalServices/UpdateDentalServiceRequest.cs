namespace DentalClinic.Contracts.Requests.DentalServices;

public class UpdateDentalServiceRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public decimal Price { get; set; }
}