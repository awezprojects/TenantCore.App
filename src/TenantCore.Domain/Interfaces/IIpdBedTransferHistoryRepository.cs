using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IIpdBedTransferHistoryRepository : IClinicRepository<IpdBedTransferHistory>
{
    Task<IpdBedTransferHistory?> GetCurrentSegmentAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<IpdBedTransferHistory>> GetByRegistrationAsync(Guid ipdRegistrationId, Guid applicationId, CancellationToken cancellationToken = default);

    // DEF-IPD-17: the FK from this table to Bed/Room/Ward is DeleteBehavior.Restrict, so deleting
    // any of the three while admission history still references it previously fell straight
    // through to a raw SqlException → an unhandled 500. These let the delete handlers check first
    // and return a clean 409 instead.
    Task<bool> HasAnyForBedAsync(Guid bedId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyForRoomAsync(Guid roomId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyForWardAsync(Guid wardId, Guid applicationId, CancellationToken cancellationToken = default);
}
