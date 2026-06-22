using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class Particular : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal DefaultAmount { get; private set; }
    public bool IsActive { get; private set; }

    private Particular() { }

    public static Particular Create(Guid applicationId, string name, decimal defaultAmount) => new()
    {
        ApplicationId = applicationId,
        Name = name,
        DefaultAmount = defaultAmount,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string name, decimal defaultAmount, bool isActive)
    {
        Name = name;
        DefaultAmount = defaultAmount;
        IsActive = isActive;
        SetUpdatedAt();
    }
}
