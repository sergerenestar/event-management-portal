namespace EventPortal.Api.Modules.Shared.Infrastructure;

public interface IBlobStorageClient
{
    Task<string> UploadAsync(string containerName, string blobName, Stream content, string contentType, CancellationToken ct);
    Task<string> GetPresignedDownloadUrlAsync(string containerName, string blobName, TimeSpan expiry, CancellationToken ct);
    Task DeleteAsync(string containerName, string blobName, CancellationToken ct);
}
