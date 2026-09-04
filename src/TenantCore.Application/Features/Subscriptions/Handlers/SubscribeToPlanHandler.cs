using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Features.Subscriptions.Commands;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Application.Services;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class SubscribeToPlanHandler(
    ISubscriptionPlanRepository planRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    IAuthApplicationService authApplicationService,
    ILogger<SubscribeToPlanHandler> logger)
    : IRequestHandler<SubscribeToPlanCommand, ClinicSubscriptionDto>
{
    public async Task<ClinicSubscriptionDto> Handle(SubscribeToPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await planRepository.GetByIdAsync(request.SubscriptionPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), request.SubscriptionPlanId);

        // Rule: Trial is once per clinic, ever — including a prior Trial that was
        // later cancelled or has since expired. Cancelling never restores the entitlement.
        if (plan.IsTrial)
        {
            var hasUsedTrial = await subscriptionRepository.HasUsedTrialAsync(request.ApplicationId, cancellationToken);
            if (hasUsedTrial)
                throw new InvalidOperationException("This clinic has already used its free trial and cannot select it again.");
        }

        // Renewal before expiry does not truncate the current term: if a currently-active
        // subscription exists, the new one starts the day after it ends. Otherwise it
        // starts now. This guarantees only one Active subscription ever covers a given moment.
        var latest = await subscriptionRepository.GetLatestForClinicAsync(request.ApplicationId, cancellationToken);
        var utcNow = DateTime.UtcNow;
        var startDate = latest is not null && latest.IsCurrentlyActive(utcNow)
            ? latest.EndDate.AddDays(1)
            : utcNow;

        var (clinicName, billingEmail, billingName) = await ResolveBillingContactAsync(request.ApplicationId, request.ActingUserId, cancellationToken);

        var subscription = ClinicSubscription.Create(request.ApplicationId, plan, startDate, clinicName, billingEmail, billingName);
        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await subscriptionRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Clinic {ApplicationId} subscribed to plan {PlanCode} ({SubscriptionId}), active {StartDate:d} to {EndDate:d}",
            request.ApplicationId, plan.Code, subscription.Id, subscription.StartDate, subscription.EndDate);

        return SubscriptionTranslator.ToDto(subscription);
    }

    // Billing contact is snapshotted from TenantCore.Auth at subscribe time — see
    // PLAN.md for why (a future notification job has no user bearer token to ask
    // Auth this later). Falls back gracefully if either Auth call comes back empty
    // so a transient Auth issue never blocks activation.
    private async Task<(string ClinicName, string BillingEmail, string BillingName)> ResolveBillingContactAsync(
        Guid applicationId, Guid actingUserId, CancellationToken ct)
    {
        var application = await authApplicationService.GetApplicationByIdAsync(applicationId, ct);
        var clinicName = application?.ApplicationName ?? string.Empty;

        var users = await authApplicationService.GetApplicationUsersAsync(applicationId, ct);
        var actingUser = users?.FirstOrDefault(u => u.UserId == actingUserId);

        var billingEmail = actingUser?.EmailId ?? application?.OfficialEmail ?? string.Empty;
        var billingName = actingUser?.FullName ?? application?.ContactPerson ?? clinicName;

        return (clinicName, billingEmail, billingName);
    }
}
