using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

/// <summary>Global catalogue reads — SubscriptionPlan is not tenant-scoped, so no method here takes an applicationId.</summary>
public interface ISubscriptionPlanRepository : IClinicRepository<SubscriptionPlan>
{
    Task<IReadOnlyList<SubscriptionPlan>> GetActivePlansAsync(CancellationToken ct = default);

    /// <summary>
    /// Only ever called with one of the four seeded codes (in practice Trial). Custom plans share
    /// Code = Custom and are never looked up this way — the unique index excludes them.
    /// </summary>
    Task<SubscriptionPlan?> GetByCodeAsync(SubscriptionPlanCode code, CancellationToken ct = default);

    /// <summary>Active and inactive, ordered by DisplayOrder then Name — the admin catalogue view.</summary>
    Task<IReadOnlyList<SubscriptionPlan>> GetAllPlansAsync(CancellationToken ct = default);

    /// <summary>Case-insensitive duplicate-name guard. excludeId lets a plan keep its own name on update.</summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken ct = default);
}
