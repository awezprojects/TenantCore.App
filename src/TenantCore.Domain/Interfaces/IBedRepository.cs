using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IBedRepository : IClinicRepository<Bed>
{
    Task<IEnumerable<Bed>> GetByRoomAsync(Guid roomId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bed>> GetAvailableByWardAsync(Guid wardId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bed>> GetByApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNumberAsync(Guid roomId, string bedNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Bed?> FindByLocationAsync(Guid applicationId, string wardName, string roomNumber, string bedNumber, CancellationToken cancellationToken = default);
}
