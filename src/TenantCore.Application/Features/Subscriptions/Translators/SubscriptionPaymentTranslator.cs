using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos.Subscriptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.Subscriptions.Translators;

public static class SubscriptionPaymentTranslator
{
    public static SubscriptionPaymentDto ToDto(SubscriptionPayment entity) => new()
    {
        Id = entity.Id,
        Purpose = entity.Purpose,
        PlanName = entity.PlanName,
        Amount = entity.Amount,
        PlanListPrice = entity.PlanListPrice,
        Currency = entity.Currency,
        Status = entity.Status,
        Method = entity.Method,
        PaymentLinkUrl = entity.Status == SubscriptionPaymentStatus.LinkCreated ? entity.PaymentLinkUrl : null,
        LinkExpiresAt = entity.Status == SubscriptionPaymentStatus.LinkCreated ? entity.LinkExpiresAt : null,
        PaidAt = entity.PaidAt,
        CreatedAt = entity.CreatedAt,
        IsAssignedByPlatform = entity.Purpose == PaymentPurpose.AdminAssigned
    };
}
