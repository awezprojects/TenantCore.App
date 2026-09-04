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
}
