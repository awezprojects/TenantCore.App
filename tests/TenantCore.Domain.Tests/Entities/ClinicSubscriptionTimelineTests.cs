using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

/// <summary>
/// The timeline rules every purchase depends on: a term bought mid-term is "upcoming", not
/// "current", and only a started term unlocks the clinic.
/// </summary>
public class ClinicSubscriptionTimelineTests
{
    private static readonly DateTime Now = new(2026, 06, 15, 10, 0, 0, DateTimeKind.Utc);

    private static SubscriptionPlan Plan(int durationDays = 30, decimal price = 999m) =>
        SubscriptionPlan.CreateForSeed(Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", "d",
            durationDays, price, "INR", isTrial: false, isPopular: false, displayOrder: 1);

    private static ClinicSubscription Subscription(DateTime startDate, int durationDays = 30) =>
        ClinicSubscription.Create(Guid.NewGuid(), Plan(durationDays), startDate, "Clinic", "billing@example.test", "Billing");

    [Fact]
    public void IsCurrentlyActive_StartedAndNotEnded_ReturnsTrue()
    {
        var subscription = Subscription(Now.AddDays(-5));

        subscription.IsCurrentlyActive(Now).Should().BeTrue();
        subscription.IsUpcoming(Now).Should().BeFalse();
    }

    [Fact]
    public void IsCurrentlyActive_StartsInTheFuture_ReturnsFalse()
    {
        // The bug this guards: a term queued after the current one used to report as active
        // immediately, so the clinic saw the wrong plan name and start date right after paying.
        var subscription = Subscription(Now.AddDays(10));

        subscription.IsCurrentlyActive(Now).Should().BeFalse();
        subscription.IsUpcoming(Now).Should().BeTrue();
    }

    [Fact]
    public void IsCurrentlyActive_AlreadyEnded_ReturnsFalse()
    {
        var subscription = Subscription(Now.AddDays(-60));

        subscription.IsCurrentlyActive(Now).Should().BeFalse();
        subscription.IsUpcoming(Now).Should().BeFalse();
    }

    [Fact]
    public void IsUpcoming_Cancelled_ReturnsFalse()
    {
        var subscription = Subscription(Now.AddDays(10));
        subscription.Cancel("admin@example.test");

        subscription.IsUpcoming(Now).Should().BeFalse();
    }

    [Fact]
    public void CreateAdminGrant_SetsZeroPriceAndGrantFields()
    {
        var subscription = ClinicSubscription.CreateAdminGrant(
            Guid.NewGuid(), Plan(price: 2499m), Now, "Clinic", "billing@example.test", "Billing",
            "admin@example.test", "Goodwill after an outage");

        subscription.PricePaid.Should().Be(0m);
        subscription.GrantedByAdminEmail.Should().Be("admin@example.test");
        subscription.GrantReason.Should().Be("Goodwill after an outage");
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void CancelUpcoming_NotStartedYet_CancelsWithReason()
    {
        var subscription = Subscription(Now.AddDays(10));

        subscription.CancelUpcoming("admin@example.test", "Doctor changed their mind", Now);

        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.CancellationReason.Should().Be("Doctor changed their mind");
        subscription.CancelledBy.Should().Be("admin@example.test");
    }

    [Fact]
    public void CancelUpcoming_AlreadyStarted_Throws()
    {
        var subscription = Subscription(Now.AddDays(-1));

        var act = () => subscription.CancelUpcoming("admin@example.test", "reason", Now);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not started*");
    }

    [Fact]
    public void Reschedule_UpcomingTerm_MovesStartAndRecomputesEndFromDuration()
    {
        var subscription = Subscription(Now.AddDays(40), durationDays: 30);
        var newStart = Now.AddDays(10);

        subscription.Reschedule(newStart, Now);

        subscription.StartDate.Should().Be(newStart);
        subscription.EndDate.Should().Be(newStart.AddDays(30), "the clinic keeps the full term it paid for");
    }

    [Fact]
    public void Reschedule_AlreadyStarted_Throws()
    {
        var subscription = Subscription(Now.AddDays(-1));

        var act = () => subscription.Reschedule(Now.AddDays(5), Now);

        act.Should().Throw<InvalidOperationException>();
    }
}
