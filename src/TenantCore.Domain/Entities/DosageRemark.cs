using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class DosageRemark : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public MedicineFormType MedicineForm { get; private set; }
    public string RemarkEnglish { get; private set; } = string.Empty;
    public string? RemarkHindi { get; private set; }
    public string? RemarkMarathi { get; private set; }
    public bool IsActive { get; private set; }

    private DosageRemark() { }

    public static DosageRemark Create(
        Guid applicationId,
        MedicineFormType medicineForm,
        string remarkEnglish,
        string? remarkHindi,
        string? remarkMarathi) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        MedicineForm = medicineForm,
        RemarkEnglish = remarkEnglish,
        RemarkHindi = remarkHindi,
        RemarkMarathi = remarkMarathi,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        MedicineFormType medicineForm,
        string remarkEnglish,
        string? remarkHindi,
        string? remarkMarathi,
        bool isActive)
    {
        MedicineForm = medicineForm;
        RemarkEnglish = remarkEnglish;
        RemarkHindi = remarkHindi;
        RemarkMarathi = remarkMarathi;
        IsActive = isActive;
        SetUpdatedAt();
    }
}
