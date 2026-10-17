using DentalClinic.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore.Storage;

namespace DentalClinic.Infrastructure.Data;

/// <summary>
/// Thin adapter that exposes EF Core's transaction through the application's
/// <see cref="IAppTransaction"/> abstraction so handlers stay persistence-agnostic.
/// </summary>
internal sealed class EfAppTransaction(IDbContextTransaction transaction) : IAppTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken) =>
        transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken) =>
        transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
