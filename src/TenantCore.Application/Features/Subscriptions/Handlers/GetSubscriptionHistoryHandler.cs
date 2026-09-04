using MediatR;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class GetSubscriptionHistoryHandler(IClinicSubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetSubscriptionHistoryQuery, IEnumerable<SubscriptionHistoryItemDto>>
{
    public async Task<IEnumerable<SubscriptionHistoryItemDto>> Handle(GetSubscriptionHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await subscriptionRepository.GetHistoryForClinicAsync(request.ApplicationId, cancellationToken);
        return history.Select(SubscriptionTranslator.ToHistoryDto);
    }
}
