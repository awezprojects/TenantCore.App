using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Application.Common;

/// <summary>
/// The one place the dummy-data prefix convention lives: what counts as a dummy record, how the
/// canonical set is created, and in what order it is cascade-deleted.
///
/// Implemented in Infrastructure because the work is bulk EF over ten tables inside a single
/// transaction, and the Application layer never touches EF. Every method scopes itself to
/// <paramref name="applicationId"/>; none of them is reachable unless DummyData:Enabled is true
/// (the controller enforces that, so the store can assume it).
/// </summary>
public interface IDummyDataStore
{
    /// <summary>Read-only: counts the dummy records this clinic currently holds.</summary>
    Task<DummyDataStatusDto> GetStatusAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>
    /// Creates the canonical dummy set for the clinic in one transaction. Throws
    /// <see cref="InvalidOperationException"/> (→ 409) when the clinic already holds dummy data —
    /// a partial or existing set must be purged, never added to.
    /// </summary>
    Task<DummyDataStatusDto> SeedAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>
    /// Cascade-deletes every dummy record and all of its children in FK-dependency order, in one
    /// transaction. Throws <see cref="InvalidOperationException"/> (→ 409) when there is nothing to purge.
    /// </summary>
    Task<DummyDataStatusDto> PurgeAsync(Guid applicationId, CancellationToken ct = default);
}
