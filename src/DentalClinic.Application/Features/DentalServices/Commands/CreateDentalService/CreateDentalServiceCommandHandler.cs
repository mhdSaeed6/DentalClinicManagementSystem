using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Application.Features.DentalServices.Mappers;
using DentalClinic.Domain.Common.Results;
using DentalClinic.Domain.Services;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.DentalServices.Commands.CreateDentalService;

public class CreateDentalServiceCommandHandler(
    ILogger<CreateDentalServiceCommandHandler> logger,
    IAppDbContext context,
    HybridCache cache)
    : IRequestHandler<CreateDentalServiceCommand, Result<DentalServiceDto>>
{
    public async Task<Result<DentalServiceDto>> Handle(CreateDentalServiceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating dental service: {Name}", request.Name);

        var serviceResult = DentalService.Create(request.Name, request.Description, request.Price);

        if (serviceResult.IsError)
        {
            logger.LogWarning("Failed to create service {Name}. Errors: {@Errors}", request.Name, serviceResult.Errors);
            return serviceResult.Errors;
        }

        var service = serviceResult.Value;

        context.DentalServices.Add(service);
        await context.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync("service", cancellationToken);

        logger.LogInformation("Service created successfully with ID: {ServiceId}", service.Id);

        return service.ToDto();
    }
}
