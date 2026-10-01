using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Common.Workflow;
using TenantCore.Application.Features.Onboarding.Emails;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Enums;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Activates a plan for a clinic at no charge. The term is queued after existing coverage exactly
/// like a paid one — it never overlaps a running term, and never leaves a gap.
/// </summary>
public sealed class GrantClinicSubscriptionHandler(
    ISubscriptionPlanRepository planRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    IWorkflowEnqueuer workflowEnqueuer,
    ILogger<GrantClinicSubscriptionHandler> logger)
    : IRequestHandler<GrantClinicSubscriptionCommand, Guid>
{
    public async Task<Guid> Handle(GrantClinicSubscriptionCommand command, CancellationToken ct)
    {
        var plan = await planRepository.GetByIdAsync(command.SubscriptionPlanId, ct);
        if (plan is null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), command.SubscriptionPlanId);

        var utcNow = DateTime.UtcNow;
        var coverageEnd = await subscriptionRepository.GetCoverageEndAsync(command.ApplicationId, utcNow, ct);
        var startDate = coverageEnd ?? utcNow;

        var subscription = ClinicSubscription.CreateAdminGrant(
            command.ApplicationId, plan, startDate,
            command.Contact.ClinicName, command.Contact.Email, command.Contact.Name,
            command.AdminEmail, command.Reason);

        await subscriptionRepository.AddAsync(subscription, ct);

        // Enqueued in the same SaveChanges as the grant itself (transactional outbox) — the email
        // can never be sent for a grant that did not commit, nor lost for one that did.
        await workflowEnqueuer.EnqueueAsync(
            WorkflowTaskType.SendEmail, $"email:subscription-granted:{subscription.Id}",
            nameof(ClinicSubscription), subscription.Id,
            OnboardingEmailTemplates.BuildPayload("SubscriptionGranted", command.Contact.Email, new
            {
                ClinicName = command.Contact.ClinicName,
                PlanName = plan.Name,
                StartDate = subscription.StartDate.ToString("dd MMM yyyy"),
                EndDate = subscription.EndDate.ToString("dd MMM yyyy")
            }),
            ct);

        await subscriptionRepository.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin granted plan {PlanId} to clinic {ApplicationId} as subscription {SubscriptionId}, running {StartDate:d} to {EndDate:d}.",
            plan.Id, command.ApplicationId, subscription.Id, subscription.StartDate, subscription.EndDate);

        return subscription.Id;
    }
}
