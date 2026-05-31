using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using TenantCore.Application.Services;

namespace TenantCore.Infrastructure.Services;

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly string _containerName;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["BlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("BlobStorage:ConnectionString is not configured.");
        _containerName = configuration["BlobStorage:ContainerName"] ?? "tenantcore";
        _serviceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(Stream stream, string blobPath, string contentType, CancellationToken ct = default)
    {
        var container = _serviceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        var blobClient = container.GetBlobClient(blobPath);
        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, ct);

        // Private container — return a read SAS URL valid for 10 years
        var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddYears(10));
        return sasUri.ToString();
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        var container = _serviceClient.GetBlobContainerClient(_containerName);
        var blobClient = container.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
