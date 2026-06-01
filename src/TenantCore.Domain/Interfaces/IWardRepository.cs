using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IWardRepository : IClinicRepository<Ward>
{
    Task<IEnumerable<Ward>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<Ward?> GetByIdWithRoomsAsync(Guid id, Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid applicationId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
