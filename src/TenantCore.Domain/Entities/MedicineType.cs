namespace TenantCore.Domain.Entities;

public class MedicineType : TenantCore.Domain.Common.AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private MedicineType() { }

    public static MedicineType Create(string name, string? description) => new()
    {
        Id = Guid.NewGuid(),
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
