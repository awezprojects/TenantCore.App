using MediatR;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class GetSubscriptionStatusHandler(
    IClinicSubscriptionRepository subscriptionRepository,
    IClinicAccountRepository accountRepository)
    : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var active = await subscriptionRepository.GetActiveForClinicAsync(request.ApplicationId, cancellationToken);
        var hasUsedTrial = await subscriptionRepository.HasUsedTrialAsync(request.ApplicationId, cancellationToken);
        var upcoming = await subscriptionRepository.GetUpcomingForClinicAsync(request.ApplicationId, utcNow, cancellationToken);
        var coverageEnd = await subscriptionRepository.GetCoverageEndAsync(request.ApplicationId, utcNow, cancellationToken);

        // Null account = never suspended, never restricted, and on the default Subscription mode.
        var account = await accountRepository.GetByApplicationIdAsync(request.ApplicationId, cancellationToken);
        var isSuspended = account?.IsSuspended ?? false;
        var isPayAsYouGo = account?.EffectiveBillingMode(utcNow) == BillingMode.PayAsYouGo;

        return SubscriptionTranslator.ToStatusDto(
            active, request.IsClinicAdmin, hasUsedTrial, utcNow,
            upcoming, coverageEnd,
            isSuspended,
            isSuspended ? account!.SuspensionMessage : null,
            isPayAsYouGo);
    }
}
