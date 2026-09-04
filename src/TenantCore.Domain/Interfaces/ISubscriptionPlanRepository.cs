using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Interfaces;

/// <summary>Global catalogue reads — SubscriptionPlan is not tenant-scoped, so no method here takes an applicationId.</summary>
public interface ISubscriptionPlanRepository : IClinicRepository<SubscriptionPlan>
{
    Task<IReadOnlyList<SubscriptionPlan>> GetActivePlansAsync(CancellationToken ct = default);
    Task<SubscriptionPlan?> GetByCodeAsync(SubscriptionPlanCode code, CancellationToken ct = default);
}
