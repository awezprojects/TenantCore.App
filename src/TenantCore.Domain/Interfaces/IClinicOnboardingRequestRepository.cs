using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

/// <summary>Not tenant-scoped — every method here filters by the requesting user, never an applicationId.</summary>
public interface IClinicOnboardingRequestRepository : IRepository<ClinicOnboardingRequest>
{
    /// <summary>The user's open request (Submitted through Provisioning), if any — enforces the one-open-request rule.</summary>
    Task<ClinicOnboardingRequest?> GetOpenForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>AsNoTracking, newest first, capped at 50.</summary>
    Task<IReadOnlyList<ClinicOnboardingRequest>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Tracked — returns null for another user's request (cross-tenant-style isolation for this user-scoped entity).</summary>
    Task<ClinicOnboardingRequest?> GetByIdForUserAsync(Guid id, Guid userId, CancellationToken ct = default);

    /// <summary>Non-terminal requests with no open WorkflowTask — the self-healing sweep's input.</summary>
    Task<IReadOnlyList<ClinicOnboardingRequest>> GetStuckAsync(CancellationToken ct = default);
}
