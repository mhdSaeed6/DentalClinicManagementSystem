using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Domain.Services;

namespace DentalClinic.Application.Features.DentalServices.Mappers;

public static class DentalServiceMappingExtensions
{
    public static DentalServiceDto ToDto(this DentalService entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new DentalServiceDto(
            entity.Id,
            entity.Name,
            entity.Description,
            entity.Price,
            entity.IsActive);
    }

    public static List<DentalServiceDto> ToDtos(this IEnumerable<DentalService> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}
