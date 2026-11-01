using TenantCore.Application.Common.Workflow;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Onboarding.Services;

public sealed class OnboardingSelfHealer(
    IClinicOnboardingRequestRepository requestRepository,
    IWorkflowTaskRepository workflowTaskRepository,
    IWorkflowEnqueuer workflowEnqueuer)
    : IOnboardingSelfHealer
{
    public async Task<int> HealStuckRequestsAsync(CancellationToken ct = default)
    {
        var stuck = await requestRepository.GetStuckAsync(ct);
        var healedCount = 0;

        foreach (var request in stuck)
        {
            if (await HealOneAsync(request.Id, ct))
                healedCount++;
        }

        return healedCount;
    }

    public async Task HealRequestAsync(Guid requestId, CancellationToken ct = default) => await HealOneAsync(requestId, ct);

    private async Task<bool> HealOneAsync(Guid requestId, CancellationToken ct)
    {
        var request = await requestRepository.GetByIdAsync(requestId, ct);
        if (request == null)
            return false;

        bool healed;
        switch (request.Status)
        {
            case ClinicOnboardingStatus.Approved:
                if (!request.CurrentPaymentId.HasValue)
                    return false;

                healed = await EnsureTaskAsync(
                    WorkflowTaskType.CreatePaymentLink, $"create-link:{request.CurrentPaymentId.Value}",
                    nameof(SubscriptionPayment), request.CurrentPaymentId.Value, ct);
                break;

            case ClinicOnboardingStatus.PaymentReceived:
            case ClinicOnboardingStatus.Provisioning:
                if (!request.ProvisionedApplicationId.HasValue)
                {
                    healed = await EnsureTaskAsync(
                        WorkflowTaskType.ProvisionClinic, $"provision:{request.Id}", nameof(ClinicOnboardingRequest), request.Id, ct);
                }
                else if (request.IsTrialGrant)
                {
                    healed = await EnsureTaskAsync(
                        WorkflowTaskType.ActivateSubscription, $"activate-subscription:trial:{request.Id}", nameof(ClinicOnboardingRequest), request.Id, ct);
                }
                else if (request.CurrentPaymentId.HasValue)
                {
                    healed = await EnsureTaskAsync(
                        WorkflowTaskType.ActivateSubscription, $"activate-subscription:{request.CurrentPaymentId.Value}",
                        nameof(SubscriptionPayment), request.CurrentPaymentId.Value, ct);
                }
                else
                {
                    return false;
                }
                break;

            case ClinicOnboardingStatus.AwaitingPayment:
                // Legitimately waiting on the doctor to pay — the reconciliation sweep, not the
                // self-healer, is what recovers a missed webhook here.
                return false;

            default:
                return false;
        }

        if (!healed)
            return false;

        await requestRepository.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Ensures the given step has an open (Pending/InProgress) task: no-ops if one already exists
    /// under its exact idempotency key, resets it to Pending if it previously failed (a bare
    /// EnqueueAsync would silently return the still-Failed row unchanged), or enqueues a fresh
    /// one. Checked by the exact idempotency key rather than "any open task for this aggregate",
    /// since the aggregate a given step is tracked under (the request itself, or its current
    /// SubscriptionPayment) varies by step and by trial-vs-paid.
    /// </summary>
    private async Task<bool> EnsureTaskAsync(WorkflowTaskType taskType, string idempotencyKey, string aggregateType, Guid aggregateId, CancellationToken ct)
    {
        var existing = await workflowTaskRepository.GetByIdempotencyKeyAsync(idempotencyKey, ct);
        if (existing == null)
        {
            await workflowEnqueuer.EnqueueAsync(taskType, idempotencyKey, aggregateType, aggregateId, ct: ct);
            return true;
        }

        if (existing.Status == WorkflowTaskStatus.Failed)
        {
            existing.ResetForManualRetry();
            return true;
        }

        return false; // Pending/InProgress already in flight, or Succeeded — nothing to do
    }
}
