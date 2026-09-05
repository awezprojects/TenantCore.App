using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IStateRepository : IRepository<State>
{
    Task<List<State>> GetAllOrderedAsync(CancellationToken ct = default);
}
