using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class Ward : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<Room> Rooms { get; private set; } = [];

    private Ward() { }

    public static Ward Create(Guid applicationId, string name, string? description) => new()
    {
        ApplicationId = applicationId,
        Name = name,
        Description = description,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
    };

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate() { IsActive = true; SetUpdatedAt(); }
}
