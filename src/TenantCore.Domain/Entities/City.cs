using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class City : BaseEntity
{
    public Guid StateId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public State State { get; private set; } = null!;

    private City() { }

    public static City Create(Guid stateId, string name) => new()
    {
        Id = Guid.NewGuid(),
        StateId = stateId,
        Name = name,
        CreatedAt = DateTime.UtcNow
    };
}
