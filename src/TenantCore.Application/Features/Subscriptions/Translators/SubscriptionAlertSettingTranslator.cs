using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Translators;

public static class SubscriptionAlertSettingTranslator
{
    public static SubscriptionAlertSettingDto ToDto(SubscriptionAlertSetting entity) => new()
    {
        Id = entity.Id,
        AlertType = entity.AlertType,
        DaysBeforeExpiry = entity.DaysBeforeExpiry,
        Subject = entity.Subject,
        Headline = entity.Headline,
        BodyMessage = entity.BodyMessage,
        IsEnabled = entity.IsEnabled,
        DisplayOrder = entity.DisplayOrder
    };

    public static void ApplyUpdate(SubscriptionAlertSetting entity, UpdateSubscriptionAlertSettingRequest request) =>
        entity.Apply(request.Subject, request.Headline, request.BodyMessage, request.IsEnabled, request.DisplayOrder);
}
