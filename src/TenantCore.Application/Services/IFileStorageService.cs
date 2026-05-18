namespace TenantCore.Application.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream fileStream, string patientFolder, string fileName, CancellationToken ct = default);
}
