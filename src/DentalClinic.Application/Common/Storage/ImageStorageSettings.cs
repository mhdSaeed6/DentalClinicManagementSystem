namespace DentalClinic.Application.Common.Storage;

/// <summary>
/// Configuration for the image store. Bound lazily so a test host's late
/// configuration sources are still observed; a blank path falls back to a
/// per-app data directory in the storage implementation.
/// </summary>
public sealed class ImageStorageSettings
{
    public const string SectionName = "Images";

    /// <summary>
    /// Root directory (absolute, or relative to the app base directory). In
    /// containers this should point at a mounted volume.
    /// </summary>
    public string? StoragePath { get; set; }
}