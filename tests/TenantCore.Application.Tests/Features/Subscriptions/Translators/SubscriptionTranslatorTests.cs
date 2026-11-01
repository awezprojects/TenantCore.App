using FluentAssertions;
using TenantCore.Application.Features.Subscriptions.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Subscriptions.Translators;

public class SubscriptionTranslatorTests
{
    private static SubscriptionPlan CreatePlan() =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Quarterly, "Quarterly", "desc", 90, 2499m, "INR", false, true, 3);

    [Fact]
    public void ToDto_ValidEntity_MapsAllProperties()
    {
        var applicationId = Guid.NewGuid();
        var plan = CreatePlan();
        var entity = ClinicSubscription.Create(applicationId, plan, DateTime.UtcNow, "Sunrise Clinic", "admin@sunrise.test", "Dr. Admin");

        var dto = SubscriptionTranslator.ToDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.ApplicationId.Should().Be(applicationId);
        dto.SubscriptionPlanId.Should().Be(plan.Id);
        dto.PlanCode.Should().Be(SubscriptionPlanCode.Quarterly);
        dto.PlanName.Should().Be("Quarterly");
        dto.PricePaid.Should().Be(2499m);
        dto.Currency.Should().Be("INR");
        dto.DurationDays.Should().Be(90);
        dto.StartDate.Should().Be(entity.StartDate);
        dto.EndDate.Should().Be(entity.EndDate);
        dto.Status.Should().Be(SubscriptionStatus.Active);
        dto.ClinicName.Should().Be("Sunrise Clinic");
        dto.BillingContactEmail.Should().Be("admin@sunrise.test");
        dto.BillingContactName.Should().Be("Dr. Admin");
    }

    [Fact]
    public void ToHistoryDto_ValidEntity_MapsDisplayFields()
    {
        var plan = CreatePlan();
        var entity = ClinicSubscription.Create(Guid.NewGuid(), plan, DateTime.UtcNow, "C", "a@b.com", "A");

        var dto = SubscriptionTranslator.ToHistoryDto(entity);

        dto.Id.Should().Be(entity.Id);
        dto.PlanName.Should().Be("Quarterly");
        dto.PricePaid.Should().Be(2499m);
        dto.StartDate.Should().Be(entity.StartDate);
        dto.EndDate.Should().Be(entity.EndDate);
        dto.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void ToPlanDto_ValidPlan_MapsAllProperties()
    {
        var plan = CreatePlan();

        var dto = SubscriptionTranslator.ToPlanDto(plan, alreadyUsed: true);

        dto.Id.Should().Be(plan.Id);
        dto.Code.Should().Be(SubscriptionPlanCode.Quarterly);
        dto.Name.Should().Be("Quarterly");
        dto.Description.Should().Be("desc");
        dto.DurationDays.Should().Be(90);
        dto.Price.Should().Be(2499m);
        dto.Currency.Should().Be("INR");
        dto.IsPopular.Should().BeTrue();
        dto.DisplayOrder.Should().Be(3);
        dto.AlreadyUsed.Should().BeTrue();
    }

    [Fact]
    public void ToStatusDto_NullActiveSubscription_ReturnsHasActiveSubscriptionFalse()
    {
        var dto = SubscriptionTranslator.ToStatusDto(null, canSubscribe: true, hasUsedTrial: false, DateTime.UtcNow);

        dto.HasActiveSubscription.Should().BeFalse();
        dto.CanSubscribe.Should().BeTrue();
        dto.DaysRemaining.Should().Be(0);
    }

    [Fact]
    public void ToStatusDto_ActiveSubscription_ComputesDaysRemainingFromEndDate()
    {
        var utcNow = new DateTime(2026, 1, 1, 8, 30, 0, DateTimeKind.Utc);
        var plan = CreatePlan();
        var entity = ClinicSubscription.Create(Guid.NewGuid(), plan, utcNow.Date.AddDays(-80), "C", "a@b.com", "A");
        // StartDate = Dec 12, EndDate = StartDate + 90 = Mar 12 -> 10 days remaining from utcNow.

        var dto = SubscriptionTranslator.ToStatusDto(entity, canSubscribe: false, hasUsedTrial: true, utcNow);

        dto.HasActiveSubscription.Should().BeTrue();
        dto.DaysRemaining.Should().Be((int)Math.Ceiling((entity.EndDate.Date - utcNow.Date).TotalDays));
        dto.CanSubscribe.Should().BeFalse();
        dto.HasUsedTrial.Should().BeTrue();
        dto.PlanName.Should().Be("Quarterly");
    }

    [Fact]
    public void ToStatusDto_NoCoverageEndSupplied_FallsBackToTheCurrentTermsOwnEnd()
    {
        var utcNow = new DateTime(2026, 1, 1, 8, 30, 0, DateTimeKind.Utc);
        var entity = ClinicSubscription.Create(Guid.NewGuid(), CreatePlan(), utcNow.Date.AddDays(-80), "C", "a@b.com", "A");

        var dto = SubscriptionTranslator.ToStatusDto(entity, canSubscribe: false, hasUsedTrial: true, utcNow);

        dto.CoverageEndDate.Should().Be(entity.EndDate);
        dto.CoverageDaysRemaining.Should().Be(dto.DaysRemaining);
    }

    [Fact]
    public void ToStatusDto_NextTermAlreadyBought_MeasuresExpiryAgainstCoverageNotThisTerm()
    {
        var utcNow = new DateTime(2026, 1, 1, 8, 30, 0, DateTimeKind.Utc);
        var current = ClinicSubscription.Create(Guid.NewGuid(), CreatePlan(), utcNow.Date.AddDays(-87), "C", "a@b.com", "A");
        var next = ClinicSubscription.Create(Guid.NewGuid(), CreatePlan(), current.EndDate, "C", "a@b.com", "A");

        var dto = SubscriptionTranslator.ToStatusDto(
            current, canSubscribe: true, hasUsedTrial: true, utcNow,
            upcoming: [next], coverageEnd: next.EndDate);

        dto.DaysRemaining.Should().BeLessThan(15, "this term is nearly over");
        dto.IsExpiringSoon.Should().BeFalse("the clinic already bought its next term");
        dto.CoverageEndDate.Should().Be(next.EndDate);
        dto.Upcoming.Should().HaveCount(1);
    }

    [Fact]
    public void ToStatusDto_Suspended_CarriesTheFlagAndMessage()
    {
        var dto = SubscriptionTranslator.ToStatusDto(
            null, canSubscribe: true, hasUsedTrial: false, DateTime.UtcNow,
            isSuspended: true, suspensionMessage: "Payment overdue");

        dto.IsSuspended.Should().BeTrue();
        dto.SuspensionMessage.Should().Be("Payment overdue");
    }

    [Fact]
    public void ToUpcomingDto_AdminGrant_IsFlaggedAsAGrant()
    {
        var granted = ClinicSubscription.CreateAdminGrant(
            Guid.NewGuid(), CreatePlan(), DateTime.UtcNow.AddDays(5), "C", "a@b.com", "A",
            "admin@example.test", "Goodwill");

        var dto = SubscriptionTranslator.ToUpcomingDto(granted);

        dto.IsGrant.Should().BeTrue();
        dto.PricePaid.Should().Be(0m);
        dto.StartDate.Should().Be(granted.StartDate);
    }

    [Fact]
    public void ToPlanDto_WithAnOffer_KeepsBothPrices()
    {
        var plan = CreatePlan();
        var validUntil = DateTime.UtcNow.AddDays(30);

        var dto = SubscriptionTranslator.ToPlanDto(plan, alreadyUsed: false,
            effectivePrice: 1999m, isSpecialOffer: true, offerValidUntil: validUntil);

        dto.Price.Should().Be(1999m);
        dto.ListPrice.Should().Be(2499m);
        dto.IsSpecialOffer.Should().BeTrue();
        dto.OfferValidUntil.Should().Be(validUntil);
    }

    [Fact]
    public void ToPlanDto_WithoutAnOffer_PriceEqualsListPrice()
    {
        var dto = SubscriptionTranslator.ToPlanDto(CreatePlan(), alreadyUsed: false);

        dto.Price.Should().Be(2499m);
        dto.ListPrice.Should().Be(2499m);
        dto.IsSpecialOffer.Should().BeFalse();
    }
}
