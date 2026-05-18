namespace TenantCore.Shared.Dtos;

public class PrescriptionReportDto
{
    public Guid Id { get; init; }
    public Guid PrescriptionId { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string StoredFileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public long? FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
}
