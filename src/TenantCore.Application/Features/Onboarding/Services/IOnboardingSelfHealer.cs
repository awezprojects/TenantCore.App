namespace TenantCore.Application.Features.Onboarding.Services;

/// <summary>
/// Invariant: every request or payment in a non-terminal state must have an open WorkflowTask.
/// Re-enqueues the correct next step for anything that doesn't — idempotent, since enqueueing
/// reuses the same idempotency key and is therefore a no-op when a task already exists. Used
/// by the reconciliation sweep and by RetryClinicOnboardingCommand.
/// </summary>
public interface IOnboardingSelfHealer
{
    /// <summary>Sweeps every stuck request. Returns how many needed healing.</summary>
    Task<int> HealStuckRequestsAsync(CancellationToken ct = default);

    /// <summary>Heals one request on demand.</summary>
    Task HealRequestAsync(Guid requestId, CancellationToken ct = default);
}
