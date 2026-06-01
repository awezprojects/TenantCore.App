using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IRoomRepository : IClinicRepository<Room>
{
    Task<IEnumerable<Room>> GetByWardAsync(Guid wardId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<Room?> GetByIdWithBedsAsync(Guid id, Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNumberAsync(Guid wardId, string roomNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
