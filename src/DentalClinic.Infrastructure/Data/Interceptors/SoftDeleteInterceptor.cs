using DentalClinic.Domain.Common;
using DentalClinic.Domain.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DentalClinic.Infrastructure.Data.Interceptors;

public class SoftDeleteInterceptor(TimeProvider dateTime) : SaveChangesInterceptor
{
    private readonly TimeProvider _dateTime = dateTime;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? context)
    {
        if (context == null)
        {
            return;
        }

        var utcNow = _dateTime.GetUtcNow();

        // 1. معالجة الكيانات الرئيسية التي تم حذفها
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = utcNow;
            }
        }

        // 2. معالجة الـ Owned Entities المرتبطة بكيانات Soft Deletable
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            foreach (var reference in entry.References)
            {
                if (reference.TargetEntry is { Entity: ISoftDeletable ownedEntity } && reference.TargetEntry.State == EntityState.Deleted)
                {
                    reference.TargetEntry.State = EntityState.Modified;
                    ownedEntity.IsDeleted = true;
                    ownedEntity.DeletedAtUtc = utcNow;
                }
            }
        }
    }
}