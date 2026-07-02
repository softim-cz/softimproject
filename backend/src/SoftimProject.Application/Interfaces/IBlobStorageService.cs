namespace SoftimProject.Application.Interfaces;

public interface IBlobStorageService
{
    /// <summary>True when a storage connection string is configured; false lets callers skip
    /// blob-dependent work (e.g. attachment import) gracefully instead of failing per item.</summary>
    bool IsConfigured { get; }

    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
