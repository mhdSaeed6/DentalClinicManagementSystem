namespace DentalClinic.Contracts.Requests.DentalServices;

public class CreateDentalServiceRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; } = null!;
    public decimal Price { get; set; }
}