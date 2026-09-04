using MediatR;
using TenantCore.Application.Features.Subscriptions.Queries;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Handlers;

public sealed class GetSubscriptionAlertSettingsHandler(ISubscriptionAlertSettingRepository settingRepository)
    : IRequestHandler<GetSubscriptionAlertSettingsQuery, IEnumerable<SubscriptionAlertSettingDto>>
{
    public async Task<IEnumerable<SubscriptionAlertSettingDto>> Handle(GetSubscriptionAlertSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingRepository.GetAllOrderedAsync(cancellationToken);
        return settings.Select(SubscriptionAlertSettingTranslator.ToDto);
    }
}
