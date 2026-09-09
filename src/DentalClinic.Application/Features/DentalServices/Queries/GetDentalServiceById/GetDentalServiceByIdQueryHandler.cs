using DentalClinic.Application.Common.Errors;
using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Features.DentalServices.Dtos;
using DentalClinic.Application.Features.DentalServices.Mappers;
using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Features.DentalServices.Queries.GetDentalServiceById;

public class GetDentalServiceByIdQueryHandler(
    ILogger<GetDentalServiceByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetDentalServiceByIdQuery, Result<DentalServiceDto>>
{
    public async Task<Result<DentalServiceDto>> Handle(GetDentalServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await context.DentalServices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DentalServiceId, cancellationToken);

        if (service is null)
        {
            logger.LogWarning("Service retrieval failed. Service with ID {ServiceId} not found.", request.DentalServiceId);
            return ApplicationErrors.ServiceNotFound;
        }

        return service.ToDto();
    }
}
