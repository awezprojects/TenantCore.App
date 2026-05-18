namespace TenantCore.Application.Services;

public interface IPdfConversionService
{
    Task<byte[]> ConvertToPdfAsync(byte[] fileBytes, string originalExtension, CancellationToken ct = default);
}
