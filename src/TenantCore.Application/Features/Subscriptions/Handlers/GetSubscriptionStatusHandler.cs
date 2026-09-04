using MediatR;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class GetSubscriptionStatusHandler(IClinicSubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto>
{
    public async Task<SubscriptionStatusDto> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var active = await subscriptionRepository.GetActiveForClinicAsync(request.ApplicationId, cancellationToken);
        var hasUsedTrial = await subscriptionRepository.HasUsedTrialAsync(request.ApplicationId, cancellationToken);

        return SubscriptionTranslator.ToStatusDto(active, request.IsClinicAdmin, hasUsedTrial, DateTime.UtcNow);
    }
}
