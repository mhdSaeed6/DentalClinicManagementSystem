using DentalClinic.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DentalClinic.Infrastructure.Data.Interceptors;

/// <summary>
/// Publishes the domain events collected on tracked aggregate roots once the
/// database changes have been committed.
///
/// Reliability note: delivery is <b>in-process and best-effort</b>. Dispatch runs
/// immediately after <c>SaveChanges</c> completes, so if the process crashes
/// between the commit and the publish - or a notification handler throws - the
/// events are lost and are not retried. Guaranteed delivery would require a
/// transactional outbox, which is intentionally out of scope.
/// </summary>
public class DispatchDomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchAsync(eventData.Context, cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();

        return base.SavedChanges(eventData, result);
    }

    private async Task DispatchAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null)
        {
            return;
        }

        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        if (entities.Count == 0)
        {
            return;
        }

        var domainEvents = entities.SelectMany(entity => entity.DomainEvents).ToList();

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
