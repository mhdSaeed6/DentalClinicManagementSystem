using DentalClinic.Application.Common.Interfaces;
using DentalClinic.Application.Common.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalClinic.Infrastructure.Storage;

/// <summary>
/// Local-disk implementation of <see cref="IImageStorage"/>. Files are stored
/// outside <c>wwwroot</c> and are only ever reached through an authenticated
/// API endpoint, so the upload directory is not directly web-servable.
///
/// Production deployments should mount a volume at the configured path; the
/// default keeps development self-contained under the app base directory.
/// </summary>
public sealed class LocalImageStorage : IImageStorage
{
    private const string DefaultFolder = "App_Data/images";

    private readonly string _root;
    private readonly ILogger<LocalImageStorage> _logger;

    public LocalImageStorage(IOptions<ImageStorageSettings> settings, ILogger<LocalImageStorage> logger)
    {
        _logger = logger;

        var configured = settings.Value.StoragePath;

        _root = Path.GetFullPath(
            string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, DefaultFolder)
                : configured is not null && Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(AppContext.BaseDirectory, configured ?? DefaultFolder));
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var now = DateTime.UtcNow;
        var relativePath = Path.Combine(
            now.ToString("yyyy"),
            now.ToString("MM"),
            $"{Guid.NewGuid():N}{extension}");

        var fullPath = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using (var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(file, cancellationToken);
        }

        return relativePath.Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(storagePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Stored image file {StoragePath} is missing.", storagePath);
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(storagePath);

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException ex)
        {
            // A missing binary must never block the row deletion; log and move on.
            _logger.LogWarning(ex, "Could not delete stored image file {StoragePath}.", storagePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "No permission to delete stored image file {StoragePath}.", storagePath);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves a storage path and refuses anything that escapes the configured
    /// root, so a tampered row cannot read outside the upload directory.
    /// </summary>
    private string Resolve(string storagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storagePath);

        var fullPath = Path.GetFullPath(Path.Combine(_root, storagePath));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The storage path escapes the configured image root.");
        }

        return fullPath;
    }
}