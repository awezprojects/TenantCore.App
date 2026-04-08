namespace TenantCore.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public string? CreatedBy { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    public void SetCreatedBy(string createdBy) => CreatedBy = createdBy;
    public void SetUpdatedBy(string updatedBy)
    {
        UpdatedBy = updatedBy;
        SetUpdatedAt();
    }
}
