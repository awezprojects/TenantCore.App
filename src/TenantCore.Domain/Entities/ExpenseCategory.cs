using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class ExpenseCategory : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private ExpenseCategory() { }

    public static ExpenseCategory Create(Guid applicationId, string name, string? description) => new()
    {
        ApplicationId = applicationId,
        Name = name,
        Description = description,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string name, string? description, bool isActive)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        SetUpdatedAt();
    }
}
