using Microsoft.Extensions.Configuration;
using TenantCore.Application.Services;

namespace TenantCore.Infrastructure.Services;

public class LocalFileStorageService(IConfiguration configuration) : IFileStorageService
{
    private readonly string _rootPath = configuration["Storage:RootPath"]
        ?? Path.Combine(AppContext.BaseDirectory, "uploads");

    public async Task<string> SaveAsync(Stream fileStream, string patientFolder, string fileName, CancellationToken ct = default)
    {
        var folder = Path.Combine(_rootPath, SanitizeFolderName(patientFolder));
        Directory.CreateDirectory(folder);

        var fullPath = Path.Combine(folder, fileName);
        await using var fileWriter = File.Create(fullPath);
        await fileStream.CopyToAsync(fileWriter, ct);

        return Path.Combine(SanitizeFolderName(patientFolder), fileName);
    }

    private static string SanitizeFolderName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid));
    }
}
