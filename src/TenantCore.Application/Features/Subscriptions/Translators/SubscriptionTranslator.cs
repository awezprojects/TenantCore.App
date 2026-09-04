using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos.Subscriptions;

namespace TenantCore.Application.Features.Subscriptions.Translators;

public static class SubscriptionTranslator
{
    // A subscription is treated as "expiring soon" once 15 days or fewer remain —
    // drives the amber tier on the dashboard banner and status pill.
    private const int ExpiringSoonThresholdDays = 15;

    public static ClinicSubscriptionDto ToDto(ClinicSubscription entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        SubscriptionPlanId = entity.SubscriptionPlanId,
        PlanCode = entity.PlanCode,
        PlanName = entity.PlanName,
        PricePaid = entity.PricePaid,
        Currency = entity.Currency,
        DurationDays = entity.DurationDays,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        Status = entity.Status,
        ClinicName = entity.ClinicName,
        BillingContactEmail = entity.BillingContactEmail,
        BillingContactName = entity.BillingContactName
    };

    public static SubscriptionHistoryItemDto ToHistoryDto(ClinicSubscription entity) => new()
    {
        Id = entity.Id,
        PlanName = entity.PlanName,
        PricePaid = entity.PricePaid,
        Currency = entity.Currency,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        Status = entity.Status
    };

    public static SubscriptionPlanDto ToPlanDto(SubscriptionPlan plan, bool alreadyUsed) => new()
    {
        Id = plan.Id,
        Code = plan.Code,
        Name = plan.Name,
        Description = plan.Description,
        DurationDays = plan.DurationDays,
        Price = plan.Price,
        Currency = plan.Currency,
        IsTrial = plan.IsTrial,
        IsPopular = plan.IsPopular,
        DisplayOrder = plan.DisplayOrder,
        AlreadyUsed = alreadyUsed
    };

    /// <summary>
    /// Builds the gate's status answer. DaysRemaining is computed as whole days
    /// between today (UTC) and EndDate — never stored — so it stays correct
    /// with no background job. Pass activeSubscription = null for a clinic that
    /// has never held one.
    /// </summary>
    public static SubscriptionStatusDto ToStatusDto(
        ClinicSubscription? activeSubscription,
        bool canSubscribe,
        bool hasUsedTrial,
        DateTime utcNow)
    {
        if (activeSubscription is null)
        {
            return new SubscriptionStatusDto
            {
                HasActiveSubscription = false,
                CanSubscribe = canSubscribe,
                HasUsedTrial = hasUsedTrial
            };
        }

        var daysRemaining = (int)Math.Ceiling((activeSubscription.EndDate.Date - utcNow.Date).TotalDays);
        daysRemaining = Math.Max(daysRemaining, 0);

        return new SubscriptionStatusDto
        {
            HasActiveSubscription = true,
            SubscriptionId = activeSubscription.Id,
            PlanCode = activeSubscription.PlanCode,
            PlanName = activeSubscription.PlanName,
            StartDate = activeSubscription.StartDate,
            EndDate = activeSubscription.EndDate,
            DaysRemaining = daysRemaining,
            IsExpiringSoon = daysRemaining <= ExpiringSoonThresholdDays,
            CanSubscribe = canSubscribe,
            HasUsedTrial = hasUsedTrial
        };
    }
}
