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

    /// <summary>True when the clinic has ANY subscription history at all, of any plan/status — the legacy-only trial rule for existing clinics once onboarding payments are required.</summary>
    Task<bool> HasAnySubscriptionHistoryAsync(Guid applicationId, CancellationToken ct = default);

    /// <summary>Idempotency lookup for activation — at most one subscription per payment.</summary>
    Task<ClinicSubscription?> GetByPaymentIdAsync(Guid subscriptionPaymentId, CancellationToken ct = default);

    /// <summary>Idempotency lookup for a trial grant — at most one subscription per onboarding request.</summary>
    Task<ClinicSubscription?> GetByOnboardingRequestIdAsync(Guid onboardingRequestId, CancellationToken ct = default);
}
