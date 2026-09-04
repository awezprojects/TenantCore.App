using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Interfaces;

/// <summary>Every method here filters by applicationId — ClinicSubscription is tenant-scoped.</summary>
public interface IClinicSubscriptionRepository : IClinicRepository<ClinicSubscription>
{
    /// <summary>The subscription (if any) currently granting access — Status Active and EndDate in the future. Used by the access guard.</summary>
    Task<ClinicSubscription?> GetActiveForClinicAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>Most recent subscription by StartDate, regardless of status — used to compute the renewal start date.</summary>
    Task<ClinicSubscription?> GetLatestForClinicAsync(Guid applicationId, CancellationToken ct = default);

    Task<IReadOnlyList<ClinicSubscription>> GetHistoryForClinicAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>True when the clinic has ever held a Trial subscription, of any status — enforces the once-per-clinic trial rule.</summary>
    Task<bool> HasUsedTrialAsync(Guid applicationId, CancellationToken ct = default);
}
