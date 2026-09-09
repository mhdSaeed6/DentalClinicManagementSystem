using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.DentalServices.Commands.RemoveDentalService;

public class RemoveDentalServiceCommandHandler(
    ILogger<RemoveDentalServiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<RemoveDentalServiceCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveDentalServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await context.DentalServices.FindAsync([request.DentalServiceId], cancellationToken);

        if (service is null)
        {
            logger.LogWarning("Service removal failed. Service with ID {ServiceId} not found.", request.DentalServiceId);
            return ApplicationErrors.ServiceNotFound;
        }

        service.Deactivate();

        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"service-{request.DentalServiceId}", cancellationToken);
        await cache.RemoveByTagAsync("service", cancellationToken);

        logger.LogInformation("Service deactivated successfully with ID: {ServiceId}", service.Id);

        return Result.Deleted;
    }
}
