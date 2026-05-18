using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class PrescriptionReport : BaseEntity
{
    public Guid PrescriptionId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public long? FileSizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private PrescriptionReport() { }

    public static PrescriptionReport Create(
        Guid prescriptionId,
        string originalFileName,
        string storedFileName,
        string filePath,
        long? fileSizeBytes) => new()
    {
        Id = Guid.NewGuid(),
        PrescriptionId = prescriptionId,
        OriginalFileName = originalFileName,
        StoredFileName = storedFileName,
        FilePath = filePath,
        FileSizeBytes = fileSizeBytes,
        UploadedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow
    };
}
