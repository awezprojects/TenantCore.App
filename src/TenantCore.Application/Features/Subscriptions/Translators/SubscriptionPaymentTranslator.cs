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
        PlanName = entity.PlanName ?? ResolvePurposeLabel(entity.Purpose),
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

    /// <summary>
    /// Phase 2 made the plan columns nullable, so a wallet top-up and a registration fee carry no
    /// plan at all. Those two purposes get a human label instead of a blank, so the payments list
    /// still reads sensibly; every plan-carrying purpose keeps its snapshotted plan name.
    /// </summary>
    private static string ResolvePurposeLabel(PaymentPurpose purpose) => purpose switch
    {
        PaymentPurpose.WalletTopUp => "Wallet top-up",
        PaymentPurpose.RegistrationFee => "Registration fee",
        _ => "—"
    };
}
