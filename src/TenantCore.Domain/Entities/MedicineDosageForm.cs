namespace TenantCore.Domain.Entities;

public class MedicineDosageForm : TenantCore.Domain.Common.AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private MedicineDosageForm() { }

    public static MedicineDosageForm Create(string name, string? description) => new()
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

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public static MedicineDosageForm CreateForSeed(Guid id, string name, string description) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        IsActive = true,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
