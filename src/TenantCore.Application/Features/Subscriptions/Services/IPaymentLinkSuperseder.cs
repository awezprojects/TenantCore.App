using TenantCore.Application.Common.Workflow;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Subscriptions.Services;

/// <summary>
/// Applies Phase 1's "at most one open clinic link" rule to a newly created payment: a link that
/// exists at the gateway is cancelled there first (that task creates the replacement's link), while
/// one that was never created can be superseded immediately.
///
/// Lives here rather than inside one handler because every purpose that creates a clinic link —
/// renewal, admin-assigned plan, wallet top-up and the registration fee — must supersede identically.
/// If the old link turns out to be paid, the cancel task keeps that payment and drops the
/// replacement, so a clinic can never pay twice.
/// </summary>
public interface IPaymentLinkSuperseder
{
    Task SupersedeOrCreateAsync(
        SubscriptionPayment? openLink, SubscriptionPayment replacement, CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class PaymentLinkSuperseder(IWorkflowEnqueuer workflowEnqueuer) : IPaymentLinkSuperseder
{
    public async Task SupersedeOrCreateAsync(
        SubscriptionPayment? openLink, SubscriptionPayment replacement, CancellationToken ct = default)
    {
        if (openLink is null || openLink.Status == SubscriptionPaymentStatus.Pending)
        {
            openLink?.MarkSuperseded(replacement.Id);

            await workflowEnqueuer.EnqueueAsync(
                WorkflowTaskType.CreatePaymentLink, $"create-link:{replacement.Id}",
                nameof(SubscriptionPayment), replacement.Id, ct: ct);
            return;
        }

        openLink.SetReplacement(replacement.Id);
        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.CancelPaymentLink, $"cancel-link:{openLink.Id}",
            nameof(SubscriptionPayment), openLink.Id, ct: ct);
    }
}
