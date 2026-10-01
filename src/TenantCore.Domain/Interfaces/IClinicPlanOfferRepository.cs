using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

public interface IClinicPlanOfferRepository : IRepository<ClinicPlanOffer>
{
    /// <summary>Active, unexpired offers for one clinic. Read-only — used to build the catalogue.</summary>
    Task<IReadOnlyList<ClinicPlanOffer>> GetLiveForClinicAsync(Guid applicationId, DateTime utcNow, CancellationToken ct = default);

    /// <summary>The clinic's current active offer for one plan, if any — the duplicate guard.</summary>
    Task<ClinicPlanOffer?> GetActiveForClinicAndPlanAsync(Guid applicationId, Guid subscriptionPlanId, CancellationToken ct = default);

    /// <summary>Tracked. Null when the offer does not exist or belongs to another clinic (treated as not found).</summary>
    Task<ClinicPlanOffer?> GetByIdForClinicAsync(Guid id, Guid applicationId, CancellationToken ct = default);
}
