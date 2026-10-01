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

    /// <summary>Price is what THIS clinic pays; ListPrice stays the catalogue price so the UI can show the saving.</summary>
    public static SubscriptionPlanDto ToPlanDto(
        SubscriptionPlan plan,
        bool alreadyUsed,
        decimal? effectivePrice = null,
        bool isSpecialOffer = false,
        DateTime? offerValidUntil = null) => new()
    {
        Id = plan.Id,
        Code = plan.Code,
        Name = plan.Name,
        Description = plan.Description,
        DurationDays = plan.DurationDays,
        Price = effectivePrice ?? plan.Price,
        ListPrice = plan.Price,
        IsSpecialOffer = isSpecialOffer,
        OfferValidUntil = offerValidUntil,
        Currency = plan.Currency,
        IsTrial = plan.IsTrial,
        IsPopular = plan.IsPopular,
        DisplayOrder = plan.DisplayOrder,
        AlreadyUsed = alreadyUsed
    };

    public static UpcomingSubscriptionDto ToUpcomingDto(ClinicSubscription entity) => new()
    {
        Id = entity.Id,
        PlanName = entity.PlanName,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        PricePaid = entity.PricePaid,
        Currency = entity.Currency,
        IsGrant = entity.GrantedByAdminEmail is not null
    };

    /// <summary>
    /// Builds the gate's status answer. Day counts are computed as whole days between today (UTC)
    /// and the relevant end date — never stored — so they stay correct with no background job.
    ///
    /// "Expiring soon" is measured against COVERAGE end, not the current term's end: a clinic that
    /// has already bought its next term is covered continuously and must not be nagged to renew.
    ///
    /// Pass activeSubscription = null for a clinic with no current term — it is then locked, even
    /// if it holds an upcoming one (that term simply has not started yet).
    /// </summary>
    public static SubscriptionStatusDto ToStatusDto(
        ClinicSubscription? activeSubscription,
        bool canSubscribe,
        bool hasUsedTrial,
        DateTime utcNow,
        IReadOnlyList<ClinicSubscription>? upcoming = null,
        DateTime? coverageEnd = null,
        bool isSuspended = false,
        string? suspensionMessage = null)
    {
        var upcomingDtos = (upcoming ?? [])
            .OrderBy(s => s.StartDate)
            .Select(ToUpcomingDto)
            .ToList();

        var coverageDaysRemaining = coverageEnd is { } end ? WholeDaysUntil(end, utcNow) : 0;

        if (activeSubscription is null)
        {
            return new SubscriptionStatusDto
            {
                HasActiveSubscription = false,
                CanSubscribe = canSubscribe,
                HasUsedTrial = hasUsedTrial,
                IsSuspended = isSuspended,
                SuspensionMessage = suspensionMessage,
                CoverageEndDate = coverageEnd,
                CoverageDaysRemaining = coverageDaysRemaining,
                Upcoming = upcomingDtos
            };
        }

        var daysRemaining = WholeDaysUntil(activeSubscription.EndDate, utcNow);

        // Without a coverage end (older callers), fall back to this term's own remaining days.
        var effectiveCoverageDays = coverageEnd is null ? daysRemaining : coverageDaysRemaining;

        return new SubscriptionStatusDto
        {
            HasActiveSubscription = true,
            SubscriptionId = activeSubscription.Id,
            PlanCode = activeSubscription.PlanCode,
            PlanName = activeSubscription.PlanName,
            StartDate = activeSubscription.StartDate,
            EndDate = activeSubscription.EndDate,
            DaysRemaining = daysRemaining,
            IsExpiringSoon = effectiveCoverageDays <= ExpiringSoonThresholdDays,
            CanSubscribe = canSubscribe,
            HasUsedTrial = hasUsedTrial,
            IsSuspended = isSuspended,
            SuspensionMessage = suspensionMessage,
            CoverageEndDate = coverageEnd ?? activeSubscription.EndDate,
            CoverageDaysRemaining = effectiveCoverageDays,
            Upcoming = upcomingDtos
        };
    }

    private static int WholeDaysUntil(DateTime endDate, DateTime utcNow)
        => Math.Max((int)Math.Ceiling((endDate.Date - utcNow.Date).TotalDays), 0);
}
