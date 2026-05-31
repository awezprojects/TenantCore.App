namespace TenantCore.Application.Services;

public interface IBlobStorageService
{
    /// <summary>Uploads a stream to the given blob path and returns the public URL.</summary>
    Task<string> UploadAsync(Stream stream, string blobPath, string contentType, CancellationToken ct = default);

    /// <summary>Deletes a blob by its path. No-op if it doesn't exist.</summary>
    Task DeleteAsync(string blobPath, CancellationToken ct = default);
}
