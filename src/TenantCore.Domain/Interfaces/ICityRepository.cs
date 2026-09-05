using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface ICityRepository : IRepository<City>
{
    Task<List<City>> GetByStateIdAsync(Guid stateId, CancellationToken ct = default);
    Task<bool> ExistsForStateAsync(Guid cityId, Guid stateId, CancellationToken ct = default);
}
