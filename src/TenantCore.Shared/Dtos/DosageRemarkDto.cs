using TenantCore.Shared.Enums;

namespace TenantCore.Shared.Dtos;

public class DosageRemarkDto
{
    public Guid Id { get; init; }
    public Guid ApplicationId { get; init; }
    public MedicineFormType MedicineForm { get; init; }
    public string RemarkEnglish { get; init; } = string.Empty;
    public string? RemarkHindi { get; init; }
    public string? RemarkMarathi { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
