using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Application.Features.DentalServices.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.DentalServices.Commands.UpdateDentalService;

public class UpdateDentalServiceCommandHandler(
    ILogger<UpdateDentalServiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<UpdateDentalServiceCommand, Result<DentalServiceDto>>
{
    public async Task<Result<DentalServiceDto>> Handle(UpdateDentalServiceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating service with ID: {ServiceId}", request.DentalServiceId);

        var service = await context.DentalServices.FindAsync([request.DentalServiceId], cancellationToken);

        if (service is null)
        {
            logger.LogWarning("Service update failed. Service with ID {ServiceId} not found.", request.DentalServiceId);
            return ApplicationErrors.ServiceNotFound;
        }

        var updateResult = service.Update(request.Name, request.Description, request.Price);

        if (updateResult.IsError)
        {
            logger.LogWarning("Failed to update service with ID {ServiceId}. Errors: {@Errors}", request.DentalServiceId, updateResult.Errors);
            return updateResult.Errors;
        }

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"service-{service.Id}", cancellationToken);
        await cache.RemoveByTagAsync("service", cancellationToken);

        logger.LogInformation("Service updated successfully with ID: {ServiceId}", service.Id);

        return service.ToDto();
    }
}
