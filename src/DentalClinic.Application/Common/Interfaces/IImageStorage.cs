namespace DentalClinic.Application.Common.Interfaces;

/// <summary>
/// Durable store for uploaded clinical images. The interface exists so the
/// local-disk implementation can be swapped for object storage (S3/Azure Blob)
/// without touching the application layer.
/// </summary>
public interface IImageStorage
{
    /// <summary>
    /// Persists <paramref name="content"/> under a server-generated name and
    /// returns the opaque storage path to record on the entity.
    /// </summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);

    /// <summary>
    /// Opens a stored image for reading, or returns <see langword="null"/> when
    /// the binary is gone.
    /// </summary>
    Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the binary. Missing files are not an error (the row may outlive
    /// the file, e.g. after a manual cleanup).
    /// </summary>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken);
}