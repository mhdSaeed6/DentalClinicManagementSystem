using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Domain.Common.Results.Abstractions;

using MediatR;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace DentalClinic.Application.Common.Behaviours;

public class CachingBehaviour<TRequest, TResponse>(HybridCache hybridCache, ILogger<CachingBehaviour<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICachedQuery cachedQuery)
        {
            return await next();
        }

        logger.LogInformation("Checking cache for {RequestName}", typeof(TRequest).Name);

        var result = await hybridCache.GetOrCreateAsync<TResponse>(
            cachedQuery.CacheKey,
            _ => new ValueTask<TResponse>((TResponse)(object)null!),
            new HybridCacheEntryOptions
            {
                Flags = HybridCacheEntryFlags.DisableUnderlyingData
            },
            cancellationToken: cancellationToken);

        if (result is null)
        {
            result = await next(cancellationToken);

            if (result is IResult res && res.IsSuccess)
            {
                logger.LogInformation("Caching result for {RequestName}", typeof(TRequest).Name);

                await hybridCache.SetAsync(
                    cachedQuery.CacheKey,
                    result,
                    new HybridCacheEntryOptions
                    {
                        Expiration = cachedQuery.Expiration
                    },
                    cachedQuery.Tags,
                    cancellationToken);
            }
        }

        return result;
    }
}