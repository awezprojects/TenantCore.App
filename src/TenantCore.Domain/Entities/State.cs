using TenantCore.Domain.Common;

namespace TenantCore.Domain.Entities;

public class State : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    public ICollection<City> Cities { get; private set; } = [];

    private State() { }

    public static State Create(string name, string code) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Code = code,
        CreatedAt = DateTime.UtcNow
    };
}
