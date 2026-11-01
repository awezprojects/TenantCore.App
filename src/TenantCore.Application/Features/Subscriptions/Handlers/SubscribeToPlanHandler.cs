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

/// <summary>
/// This free-activation endpoint now only ever grants the Trial plan, and only to a legacy
/// clinic with no subscription history at all — every clinic created through the onboarding
/// workflow already has history (its trial or its paid plan) by the time it could reach here.
/// Paid plans are activated exclusively through a confirmed Razorpay payment link.
/// </summary>
public sealed class SubscribeToPlanHandler(
    ISubscriptionPlanRepository planRepository,
    IClinicSubscriptionRepository subscriptionRepository,
    IClinicAccountRepository accountRepository,
    IAuthApplicationService authApplicationService,
    ILogger<SubscribeToPlanHandler> logger)
    : IRequestHandler<SubscribeToPlanCommand, ClinicSubscriptionDto>
{
    public async Task<ClinicSubscriptionDto> Handle(SubscribeToPlanCommand request, CancellationToken cancellationToken)
    {
        if (await accountRepository.IsSuspendedAsync(request.ApplicationId, cancellationToken))
            throw new InvalidOperationException("This clinic is suspended. Contact CloudClinic support.");

        var plan = await planRepository.GetByIdAsync(request.SubscriptionPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
            throw new NotFoundException(nameof(SubscriptionPlan), request.SubscriptionPlanId);

        if (!plan.IsTrial)
            throw new InvalidOperationException(
                "Paid plans are activated through a payment link, not this endpoint. Get a payment link from the subscription page.");

        var hasAnyHistory = await subscriptionRepository.HasAnySubscriptionHistoryAsync(request.ApplicationId, cancellationToken);
        if (hasAnyHistory)
            throw new InvalidOperationException("This clinic already has subscription history and cannot select the free trial.");

        var (clinicName, billingEmail, billingName) = await ResolveBillingContactAsync(request.ApplicationId, request.ActingUserId, cancellationToken);

        var subscription = ClinicSubscription.Create(
            request.ApplicationId, plan, DateTime.UtcNow, clinicName, billingEmail, billingName,
            purchasedByUserId: request.ActingUserId);

        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await subscriptionRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Clinic {ApplicationId} activated the free trial ({SubscriptionId}), active {StartDate:d} to {EndDate:d}",
            request.ApplicationId, subscription.Id, subscription.StartDate, subscription.EndDate);

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
