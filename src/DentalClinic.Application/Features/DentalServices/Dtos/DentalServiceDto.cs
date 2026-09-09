namespace DentalClinic.Application.Features.DentalServices.Dtos;

public sealed record DentalServiceDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    bool IsActive);
