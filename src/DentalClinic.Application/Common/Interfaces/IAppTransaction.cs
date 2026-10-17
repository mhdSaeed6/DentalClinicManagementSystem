namespace DentalClinic.Application.Common.Interfaces;

/// <summary>
/// Minimal unit-of-work abstraction so a handler can wrap a multi-step write
/// (check, then insert) in a single atomic operation without taking a
/// dependency on EF Core's transaction types.
/// </summary>
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
